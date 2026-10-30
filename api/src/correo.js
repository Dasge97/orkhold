// Correos de cuenta (14-cuentas.md §3): por SMTP si hay SMTP_URL; si no, el enlace va al registro de la API.
import nodemailer from 'nodemailer';

let transporte = null;

/** Lo que se ha "mandado" sin SMTP (para los tests y para probar en local). */
export const enviados = [];

export function configurarCorreo(smtpUrl) {
  transporte = smtpUrl ? nodemailer.createTransport(smtpUrl) : null;
}

export async function mandar({ para, asunto, texto, registro }) {
  const remitente = process.env.CORREO_REMITENTE || 'Orkhold <no-responder@orkhold.com>';
  if (!transporte) {
    enviados.push({ para, asunto, texto });
    if (enviados.length > 50) enviados.shift();
    registro?.info({ para, asunto, texto }, 'Correo sin SMTP (solo registro)');
    return;
  }
  await transporte.sendMail({ from: remitente, to: para, subject: asunto, text: texto });
}
