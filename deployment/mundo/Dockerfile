# Servidor de mundo del pueblo (fase 7.7): la compilación "Dedicated Server" de Linux de Unity.
# Antes de construir: compilar con CompilarEscritorio.ServidorLinuxEnBatch y copiar
# Builds/Servidor/ a deployment/mundo/servidor/ (lo hace deployment/desplegar.sh).
FROM debian:bookworm-slim
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates \
 && rm -rf /var/lib/apt/lists/* \
 && useradd --create-home mundo
WORKDIR /mundo
COPY --chown=mundo servidor/ ./
RUN chmod +x orkhold-mundo.x86_64
USER mundo
EXPOSE 7777/udp
# -logFile - : el registro de Unity sale por la salida estándar (docker logs).
ENTRYPOINT ["./orkhold-mundo.x86_64", "-batchmode", "-nographics", "-servidor", "-puerto", "7777", "-logFile", "-"]
