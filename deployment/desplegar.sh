#!/usr/bin/env bash
# Despliega Orkhold en codehive desde el PC (Git Bash). Sin argumentos: todo.
#   deployment/desplegar.sh            sube api/, deployment/ y el servidor de mundo, y arranca
#   deployment/desplegar.sh --sin-mundo  no vuelve a subir la compilación del servidor de mundo
# Antes: compilar el servidor (Linux: CompilarEscritorio.ServidorLinuxEnBatch; o Windows para Wine:
# CompilarEscritorio.WindowsEnBatch). Los secretos los crea o lee orkhold-secretos en el servidor.
set -euo pipefail
cd "$(dirname "$0")/.."
REMOTO=/home/codehive/codehive-app-state/deployments/orkhold
W=$REMOTO/workspace
SUBIR_MUNDO=1; [ "${1:-}" = "--sin-mundo" ] && SUBIR_MUNDO=0

echo "1/4 Subiendo api/ y deployment/..."
ssh codehive "mkdir -p $W $REMOTO/estado /home/codehive/backups/orkhold /home/codehive/logs"
tar -czf - --exclude=node_modules --exclude=.env --exclude='deployment/mundo/servidor' --exclude='deployment/estado' api deployment \
  | ssh codehive "tar -xzf - -C $W"

if [ $SUBIR_MUNDO = 1 ]; then
  if [ -f Builds/Servidor/orkhold-mundo.x86_64 ]; then ORIGEN=Builds/Servidor; DF=Dockerfile
  else ORIGEN=Builds/Windows; DF=Dockerfile.wine; fi
  echo "2/4 Subiendo el servidor de mundo ($ORIGEN, $DF)..."
  ssh codehive "rm -rf $W/deployment/mundo/servidor && mkdir -p $W/deployment/mundo/servidor"
  tar -czf - -C "$ORIGEN" --exclude='*_DoNotShip' --exclude='*_BackUpThisFolder_ButDontShipItWithYourGame' . \
    | ssh codehive "tar -xzf - -C $W/deployment/mundo/servidor"
else
  echo "2/4 Servidor de mundo: se deja el que hay."
fi

echo "3/4 Scripts, secretos, proxy y cron..."
ssh codehive bash -s <<REMOTO_FIN
set -euo pipefail
chmod +x $W/deployment/codehive/orkhold-*
for s in orkhold-secretos orkhold-vigilar orkhold-copia orkhold-admin; do ln -sf $W/deployment/codehive/\$s /home/codehive/bin/\$s; done
/home/codehive/bin/orkhold-secretos generar
if [ -f $W/deployment/mundo/servidor/orkhold-mundo.x86_64 ]; then
  sed -i 's/^MUNDO_DOCKERFILE=.*/MUNDO_DOCKERFILE=Dockerfile/; s#^MUNDO_ESTADO=.*#MUNDO_ESTADO=/estado/mundo.json#' $W/deployment/.env
fi
cp $W/deployment/codehive/deployment-orkhold.yml /home/codehive/infrastructure/traefik/dynamic/deployment-orkhold.yml
( crontab -l 2>/dev/null | grep -vi 'orkhold' ; \
  echo '# Orkhold: estado de la API y del servidor de mundo para el Kit, cada 5 minutos.'; \
  echo '*/5 * * * * /home/codehive/bin/orkhold-vigilar >> /home/codehive/logs/orkhold-vigilar.log 2>&1'; \
  echo '# Orkhold: copia diaria de PostgreSQL en /home/codehive/backups/orkhold (14 días).'; \
  echo '15 4 * * * /usr/bin/flock -n /tmp/orkhold-copia.lock /home/codehive/bin/orkhold-copia >> /home/codehive/logs/orkhold-copia.log 2>&1' ) | crontab -
( crontab -l; ) | grep -c orkhold- | xargs echo "Líneas de cron de Orkhold:"
REMOTO_FIN

echo "4/4 Construyendo y arrancando contenedores..."
ssh codehive "cd $W/deployment && docker compose --env-file .env up -d --build 2>&1 | tail -5 && docker compose ps --format '{{.Name}} {{.State}}'"
