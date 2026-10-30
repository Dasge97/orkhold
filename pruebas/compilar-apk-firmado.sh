#!/usr/bin/env bash
# Compila el APK firmado de Orkhold contra el servidor de codehive (tarea 9.5).
# La contraseña del almacén se lee de la bóveda sin mostrarla y solo vive en el entorno de Unity.
#   pruebas/compilar-apk-firmado.sh [url-api] [host:puerto-mundo]
set -euo pipefail
cd "$(dirname "$0")/.."
URL="${1:-https://app.orkhold.com}"
MUNDO="${2:-app.orkhold.com:7777}"
export ORKHOLD_KEYSTORE="$(cygpath -w "$USERPROFILE/.orkhold/orkhold.keystore")"
[ -f "$USERPROFILE/.orkhold/orkhold.keystore" ] || { echo "Falta el almacén: pruebas/crear-keystore.sh" >&2; exit 1; }
ORKHOLD_KEYSTORE_CLAVE=$(ssh codehive '/home/codehive/bin/vault-usar "orkhold - keystore" CLAVE -- printenv CLAVE') \
  || { echo "No se pudo leer la contraseña (¿bóveda bloqueada?)" >&2; exit 1; }
export ORKHOLD_KEYSTORE_CLAVE
"/c/Program Files/Unity/Hub/Editor/6000.5.9f1/Editor/Unity.exe" -batchmode -projectPath "$(cygpath -w "$PWD")" \
  -logFile "$(cygpath -w "$PWD/Builds/apk-firmado.log")" -buildTarget Android -executeMethod CompilarAndroid.EnBatch \
  -urlApi "$URL" -mundoApk "$MUNDO" || true
grep -E "APK: (firma|urlApi|resultado)" Builds/apk-firmado.log
