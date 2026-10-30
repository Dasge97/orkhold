#!/usr/bin/env bash
# Crea el almacén de claves de Android de Orkhold (tarea 9.5) y guarda su contraseña en la bóveda
# como "orkhold - keystore". La contraseña se genera aquí y no se muestra. Necesita la bóveda
# desbloqueada (ssh codehive vault-desbloquear). El almacén queda en %USERPROFILE%/.orkhold/, fuera
# del repositorio; sin él no se pueden publicar actualizaciones de la app: hacer copia.
set -euo pipefail
ALMACEN="$USERPROFILE/.orkhold/orkhold.keystore"
KEYTOOL="/c/Program Files/Unity/Hub/Editor/6000.5.9f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe"
[ -f "$ALMACEN" ] && { echo "Ya existe $ALMACEN; no se toca."; exit 0; }
ssh codehive /home/codehive/bin/vault-listar orkhold >/dev/null || { echo "La bóveda está bloqueada: ssh codehive vault-desbloquear" >&2; exit 1; }
mkdir -p "$(dirname "$ALMACEN")"
CLAVE=$(openssl rand -hex 20)
# Primero se guarda la contraseña: si falla, no queda un almacén sin contraseña conocida.
printf '%s' "$CLAVE" | ssh codehive '/home/codehive/bin/vault-guardar "orkhold - keystore" --usuario orkhold --url https://github.com/Dasge97/orkhold' >/dev/null
CLAVE_ENV="$CLAVE" "$KEYTOOL" -genkeypair -keystore "$(cygpath -w "$ALMACEN")" -alias orkhold -keyalg RSA -keysize 2048   -validity 10000 -storepass:env CLAVE_ENV -keypass:env CLAVE_ENV -dname "CN=Orkhold, O=Orkhold, C=ES" >/dev/null
echo "Almacén creado en $ALMACEN; contraseña en la bóveda: orkhold - keystore"
