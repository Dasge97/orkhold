// Saqueo (08-formato-y-pvp.md §4). Sustituye a los asaltos de 90 s.
// - POST /saqueo/preparar: elige rival (puntuación parecida, sin escudo, no saqueado por ti en 24 h) o un fuerte
//   de prueba; dice cuántas piezas y escondites a la vista tiene.
// - POST /saqueo/empezar: cobra la tasa y las herramientas y devuelve lo que se ve del fuerte.
// - POST /saqueo/mirar: "Pesa.", "Suena hueco.", "Tierra normal." o "Tierra removida." (con farol).
// - POST /saqueo/resolver: rehace la exploración, comprueba las marcas, mueve el botín, Elo, tasa y escudo.
// - GET /saqueo/informes y POST /saqueo/informes/visto: lo que ve el defensor.  - GET /saqueo/historial.
import { z } from 'zod';
import { validar } from '../servidor.js';
import { enTransaccion } from '../bd.js';
import { B } from '../balance.js';
import { leerJugador, leerInventario, leerDisenoParaCambiar, guardarDiseno, sumar, inicioDiaSaqueos, ErrorJuego } from '../inventario.js';
import { fuerteSistema, limpiarDiseno, construible, maxSaqueosDia, HERRAMIENTAS } from '../reglas.js';
import { RECURSOS, vacio, meter, unidades, ocupacion } from '../escondites.js';
import { visible, resumenVisible, rehacer, alcanzables, marcable, esconditeEn, botinDe, mirar, posicionEn, tramo } from '../saqueo.js';

const sesion = { config: { sesion: true } };
const Id = z.union([z.string(), z.number()]).transform(String);
const Casilla = z.object({ x: z.number().int().min(0).max(40), z: z.number().int().min(0).max(40) });
const Entrada = z.object({
  t: z.number().min(0).max(1000), x: z.number().min(-5).max(45), z: z.number().min(-5).max(45),
  golpe: Casilla.optional(),
  // El móvil (JsonUtility) siempre manda "golpe"; "golpea" dice si va de verdad.
  golpea: z.boolean().optional(),
}).transform(e => (e.golpea === false ? { ...e, golpe: undefined } : e));
const MINUTOS_PARA_RESOLVER = 10;

/** Fuerte de prueba: el del sistema con escondites de mentira (no hay botín real). */
function fuertePrueba(puntuacion, azar = Math.random) {
  const s = fuerteSistema(puntuacion);
  const d = limpiarDiseno(s.diseno);
  const ocupadas = new Set(d.piezas.map(q => `${q.x},${q.z}`));
  const libres = [];
  for (let x = 1; x < 15; x++) for (let zz = 1; zz < 15; zz++) if (construible(x, zz) && !ocupadas.has(`${x},${zz}`)) libres.push([x, zz]);
  const cont = () => ({ ...vacio(), moneda: 2 + Math.floor(azar() * 10), madera: Math.floor(azar() * 6) });
  for (const [tipo, n] of [['cofre', 2], ['barril', 2], ['enterrado', 2]])
    for (let i = 0; i < n && libres.length; i++) {
      const [x, zz] = libres.splice(Math.floor(azar() * libres.length), 1)[0];
      // La mitad de los escondites, vacíos: así se practica mirar.
      d.piezas.push({ x, z: zz, tipo, vida_extra: 0, contenido: azar() < 0.5 ? cont() : vacio() });
    }
  d.central = { ...vacio(), moneda: 12 };
  return { nombre: s.nombre, puntos: s.puntos, diseno: d };
}

/** Saqueos de hoy (desde la hora de recarga) que ya han empezado. */
async function saqueosHoy(c, jugadorId) {
  return (await c.query(
    `SELECT count(*)::int AS n FROM saqueos WHERE atacante_id = $1 AND estado <> 'preparado' AND creado >= $2`,
    [jugadorId, inicioDiaSaqueos()])).rows[0].n;
}

export default async function (app) {
  // ---------- preparar ----------

  app.post('/saqueo/preparar', sesion, async (req) => {
    // Con la API en modo de pruebas (solo en local), se puede pedir un rival por nombre.
    const pedido = app.opciones.pruebas ? String(req.body?.rival ?? '') : '';
    return enTransaccion(app.pool, async (c) => {
      const yo = (await c.query('SELECT id, puntuacion FROM jugadores WHERE id = $1 FOR UPDATE', [req.jugadorId])).rows[0];
      const hoy = await saqueosHoy(c, yo.id);
      if (hoy >= maxSaqueosDia()) throw new ErrorJuego('limite_diario', `Ya has hecho ${maxSaqueosDia()} saqueos hoy. Se recargan a las ${B('saqueo.hora_recarga')}:00 (UTC)`, 429);
      // Un saqueo preparado y no empezado se descarta al preparar otro.
      await c.query(`DELETE FROM saqueos WHERE atacante_id = $1 AND estado = 'preparado'`, [yo.id]);

      const rivales = (await c.query(
        `SELECT j.id, j.nombre, j.puntuacion, j.estandarte, f.diseno_json
           FROM jugadores j JOIN fuertes f ON f.jugador_id = j.id
          WHERE j.id <> $1 AND j.nombre IS NOT NULL AND j.bloqueado IS NULL
            AND (j.escudo_hasta IS NULL OR j.escudo_hasta < now())
            AND abs(j.puntuacion - $2) <= $3
            AND ($5 = '' OR j.nombre = $5)
            AND NOT EXISTS (SELECT 1 FROM saqueos s WHERE s.atacante_id = $1 AND s.defensor_id = j.id
                              AND s.estado <> 'preparado' AND s.creado > now() - make_interval(hours => $4))
          ORDER BY random() LIMIT 20`,
        [yo.id, yo.puntuacion, B('saqueo.margen_rival'), B('saqueo.horas_mismo_rival'), pedido])).rows;
      // Solo vale un rival que tenga algo guardado (decisión 115).
      const rival = rivales.find(r => ocupacion(limpiarDiseno(r.diseno_json)).guardado > 0);
      let defensor = null, prueba = false, diseno, nombre, puntos, estandarte = '';
      if (rival) {
        defensor = rival.id; diseno = limpiarDiseno(rival.diseno_json); nombre = rival.nombre; puntos = rival.puntuacion; estandarte = rival.estandarte ?? '';
      } else {
        const p = fuertePrueba(yo.puntuacion);
        prueba = true; diseno = p.diseno; nombre = `Fuerte de prueba (${p.nombre})`; puntos = p.puntos;
      }
      const s = (await c.query(
        `INSERT INTO saqueos (atacante_id, defensor_id, prueba, diseno_defensor_json, tasa) VALUES ($1, $2, $3, $4, $5) RETURNING id`,
        [yo.id, defensor, prueba, JSON.stringify(diseno), prueba ? 0 : B('saqueo.tasa')])).rows[0];
      const inv = await leerInventario(c, yo.id);
      return {
        saqueo_id: String(s.id),
        rival: { nombre, puntuacion: puntos, estandarte, tramo: tramo(puntos), prueba, ...resumenVisible(diseno) },
        tasa: prueba ? 0 : B('saqueo.tasa'),
        herramientas: HERRAMIENTAS.filter(h => B(`herramienta.${h}.disponible`) && inv[h] > 0).map(h => ({ id: h, cantidad: inv[h] })),
        max_herramientas: B('saqueo.max_herramientas'),
        saqueos_hoy: hoy,
        max_saqueos_dia: maxSaqueosDia(),
      };
    });
  });

  // ---------- empezar ----------

  app.post('/saqueo/empezar', sesion, async (req) => {
    const { saqueo_id, herramientas } = validar(z.object({
      saqueo_id: Id, herramientas: z.array(z.enum(HERRAMIENTAS)).max(5).default([]),
    }), req.body);
    const usadas = [...new Set(herramientas)];
    if (usadas.length > B('saqueo.max_herramientas')) throw new ErrorJuego('demasiadas_herramientas', `Como mucho ${B('saqueo.max_herramientas')} herramientas`);
    return enTransaccion(app.pool, async (c) => {
      const s = (await c.query('SELECT * FROM saqueos WHERE id = $1 FOR UPDATE', [saqueo_id])).rows[0];
      if (!s || String(s.atacante_id) !== String(req.jugadorId)) throw new ErrorJuego('saqueo_no_existe', 'Ese saqueo no es tuyo', 404);
      if (s.estado !== 'preparado') throw new ErrorJuego('saqueo_empezado', 'Ese saqueo ya ha empezado', 409);
      if (await saqueosHoy(c, req.jugadorId) >= maxSaqueosDia()) throw new ErrorJuego('limite_diario', 'Ya has hecho todos los saqueos de hoy', 429);
      for (const h of usadas)
        if (!B(`herramienta.${h}.disponible`)) throw new ErrorJuego('herramienta_bloqueada', `${h}: todavía no se puede usar`);
      // La tasa se paga de los escondites; las herramientas se gastan.
      const delta = Object.fromEntries(usadas.map(h => [h, -1]));
      if (s.tasa) delta.moneda = -s.tasa;
      try { await sumar(c, req.jugadorId, delta); }
      catch (e) {
        if (e.codigo === 'recursos_insuficientes') throw new ErrorJuego('recursos_insuficientes', s.tasa ? `Necesitas ${s.tasa} monedas para la tasa y las herramientas que elijas` : e.message, 409);
        throw e;
      }
      const acciones = B('saqueo.acciones') + (usadas.includes('cuarta_accion') ? 1 : 0);
      await c.query(
        `UPDATE saqueos SET estado = 'explorando', herramientas = $2, acciones = $3, empezado = now() WHERE id = $1`,
        [s.id, JSON.stringify(usadas), acciones]);
      return {
        saqueo_id: String(s.id),
        diseno: visible(s.diseno_defensor_json),
        exploracion_segundos: B('saqueo.exploracion_segundos'),
        mirar_segundos: B('saqueo.mirar_segundos'),
        acciones,
        herramientas: usadas,
        jugador: await leerJugador(c, req.jugadorId),
      };
    });
  });

  // ---------- mirar ----------

  app.post('/saqueo/mirar', sesion, async (req) => {
    const { saqueo_id, x, z: zz, t } = validar(z.object({ saqueo_id: Id, x: z.number().int(), z: z.number().int(), t: z.number().min(0).max(1000) }), req.body);
    return enTransaccion(app.pool, async (c) => {
      const s = (await c.query(
        `SELECT *, extract(epoch FROM now() - empezado)::float8 AS pasados FROM saqueos WHERE id = $1 FOR UPDATE`, [saqueo_id])).rows[0];
      if (!s || String(s.atacante_id) !== String(req.jugadorId)) throw new ErrorJuego('saqueo_no_existe', 'Ese saqueo no es tuyo', 404);
      if (s.estado !== 'explorando') throw new ErrorJuego('saqueo_no_explorando', 'Ese saqueo no está en la exploración', 409);
      if (s.pasados > B('saqueo.exploracion_segundos') + 5) throw new ErrorJuego('exploracion_terminada', 'Se acabó el tiempo de explorar', 409);
      if (s.miradas.length >= 200) throw new ErrorJuego('demasiadas_miradas', 'Has mirado demasiadas cosas', 429);
      const respuesta = mirar(s.diseno_defensor_json, x, zz, s.herramientas.includes('farol'));
      await c.query(`UPDATE saqueos SET miradas = miradas || $2::jsonb WHERE id = $1`,
        [s.id, JSON.stringify([{ x, z: zz, t: Math.min(t, s.pasados + 2), respuesta }])]);
      return { respuesta };
    });
  });

  // ---------- resolver ----------

  app.post('/saqueo/resolver', sesion, async (req) => {
    const { saqueo_id, marcas, entradas } = validar(z.object({
      saqueo_id: Id,
      marcas: z.array(Casilla).max(10).default([]),
      entradas: z.array(Entrada).max(4000).default([]),
    }), req.body);
    return enTransaccion(app.pool, async (c) => {
      const s = (await c.query(
        `SELECT *, extract(epoch FROM now() - empezado)::float8 AS pasados FROM saqueos WHERE id = $1 FOR UPDATE`, [saqueo_id])).rows[0];
      if (!s || String(s.atacante_id) !== String(req.jugadorId)) throw new ErrorJuego('saqueo_no_existe', 'Ese saqueo no es tuyo', 404);
      if (s.estado !== 'explorando') throw new ErrorJuego('saqueo_no_explorando', 'Ese saqueo no está en marcha', 409);
      const duracion = B('saqueo.exploracion_segundos');
      // Pasado el tiempo de resolver, cuenta como "no encontró nada" (se pierde la tasa).
      const tarde = s.pasados > duracion + MINUTOS_PARA_RESOLVER * 60;
      const diseno = s.diseno_defensor_json;

      // 1. Rehacer la exploración: no se acepta más tiempo del que ha pasado desde "empezar".
      const limite = Math.min(duracion, s.pasados + 3);
      const usarPico = s.herramientas.includes('pico');
      const picoHasta = B('herramienta.pico.segundos');
      const r = rehacer(diseno, tarde ? [] : entradas.filter(e => e.t <= limite + 2), {
        duracion: limite, danoGolpe: t => (usarPico && t <= picoHasta ? B('herramienta.pico.dano') : 1),
      });
      let descartadas = r.descartadas + entradas.filter(e => e.t > limite + 2).length;
      // Las miradas tienen que haberse hecho al lado de lo mirado.
      for (const m of s.miradas) {
        const p = posicionEn(r.posiciones, m.t);
        if (Math.max(Math.abs(p.x - m.x), Math.abs(p.z - m.z)) > 1.8) descartadas++;
      }

      // 2. Marcas: sin repetir, como mucho las acciones, y con camino libre.
      const alcanzadas = alcanzables(diseno, r.rotos);
      const vistas = new Set();
      const lista = [];
      for (const m of tarde ? [] : marcas) {
        const k = `${m.x},${m.z}`;
        if (vistas.has(k) || lista.length >= s.acciones) continue;
        vistas.add(k);
        lista.push(m);
      }

      // 3. Resolver cada marca contra el fuerte de verdad (o el de prueba, que no es de nadie).
      const real = !s.prueba && s.defensor_id;
      const defensor = real ? await leerDisenoParaCambiar(c, s.defensor_id) : limpiarDiseno(diseno);
      const botin = vacio();
      const resultados = [];
      let aciertos = 0, centralGrande = false;
      for (const m of lista) {
        if (!marcable(m.x, m.z, alcanzadas)) { resultados.push({ x: m.x, z: m.z, resultado: 'inalcanzable', botin: vacio() }); continue; }
        const e = esconditeEn(defensor, m.x, m.z);
        if (!e) { resultados.push({ x: m.x, z: m.z, resultado: 'nada', botin: vacio() }); continue; }
        if (unidades(e.contenido) === 0) { resultados.push({ x: m.x, z: m.z, resultado: 'vacio', tipo: e.tipo, botin: vacio() }); continue; }
        if (e.tipo === 'central' && unidades(e.contenido) >= B('saqueo.central_umbral')) centralGrande = true;
        const b = botinDe(e);
        for (const rr of RECURSOS) { e.dueno[e.campo][rr] -= b[rr]; botin[rr] += b[rr]; }
        if (unidades(b) > 0) aciertos++;
        resultados.push({ x: m.x, z: m.z, resultado: unidades(b) > 0 ? 'botin' : 'vacio', tipo: e.tipo, botin: b });
      }

      // 4. Puntos, tasa, escudo y botín (no en el fuerte de prueba).
      const yo = (await c.query('SELECT puntuacion FROM jugadores WHERE id = $1 FOR UPDATE', [req.jugadorId])).rows[0];
      let pa = 0, pd = 0, perdido = 0, puntosDefAntes = null;
      if (real) {
        const tipo = aciertos === 0 ? 'nada' : aciertos === 1 ? 'una' : 'varias';
        pa = B(`saqueo.elo_${tipo}_atacante`); pd = B(`saqueo.elo_${tipo}_defensor`);
        if (centralGrande) { pa += B('saqueo.central_extra'); pd -= B('saqueo.central_extra'); }
        const def = (await c.query('SELECT puntuacion FROM jugadores WHERE id = $1 FOR UPDATE', [s.defensor_id])).rows[0];
        puntosDefAntes = def.puntuacion;
        // Lo que se lleva sale del fuerte del defensor; si no encontró nada, la tasa va al defensor.
        if (aciertos === 0 && s.tasa) meter(defensor, { moneda: s.tasa });
        await guardarDiseno(c, s.defensor_id, defensor);
        await c.query(`UPDATE jugadores SET puntuacion = greatest(0, puntuacion + $2),
                         escudo_hasta = CASE WHEN $3 THEN now() + make_interval(hours => $4) ELSE escudo_hasta END WHERE id = $1`,
          [s.defensor_id, pd, aciertos > 0, B('saqueo.escudo_horas')]);
        await c.query('UPDATE jugadores SET puntuacion = greatest(0, puntuacion + $2) WHERE id = $1', [req.jugadorId, pa]);
        perdido = (await sumar(c, req.jugadorId, botin)).perdido;
      }
      const rotos = [...r.rotos].map(k => { const [x, zz] = k.split(',').map(Number); return { x, z: zz }; });
      await c.query(
        `UPDATE saqueos SET estado = 'terminado', terminado = now(), marcas_json = $2, marcas = $3, encontrado = $4, descartadas = $5,
                segundos_exploracion = $6, rotos_json = $7, entradas_json = $8, botin_json = $9, puntos_atacante_antes = $10,
                puntos_atacante = $11, puntos_defensor_antes = $12, puntos_defensor = $13 WHERE id = $1`,
        [s.id, JSON.stringify(resultados), lista.length, aciertos, descartadas, Math.round(r.ultimo.t),
          JSON.stringify(rotos), JSON.stringify(entradas.slice(0, 1500)),
          JSON.stringify(real ? botin : vacio()), yo.puntuacion, pa, puntosDefAntes, real ? pd : null]);
      return {
        prueba: !real,
        tarde,
        marcas: resultados,
        botin: real ? botin : vacio(),
        botin_prueba: real ? undefined : botin,
        perdido,
        rotos,
        descartadas,
        puntos_antes: yo.puntuacion,
        puntos_despues: Math.max(0, yo.puntuacion + pa),
        tasa_al_defensor: real && aciertos === 0 ? s.tasa : 0,
        jugador: await leerJugador(c, req.jugadorId),
      };
    });
  });

  // ---------- informes e historial ----------

  app.get('/saqueo/informes', sesion, async (req) => {
    const filas = (await app.pool.query(
      `SELECT s.*, a.nombre AS atacante FROM saqueos s LEFT JOIN jugadores a ON a.id = s.atacante_id
        WHERE s.defensor_id = $1 AND s.estado = 'terminado' ORDER BY s.id DESC LIMIT 20`, [req.jugadorId])).rows;
    return {
      informes: filas.map(f => ({
        id: String(f.id), atacante: f.atacante ?? 'Cuenta borrada', creado: f.creado.toISOString(), visto: f.visto_defensor,
        rotos: f.rotos_json ?? [], miradas: (f.miradas ?? []).map(m => ({ x: m.x, z: m.z })),
        marcas: (f.marcas_json ?? []).map(m => ({ x: m.x, z: m.z, resultado: m.resultado, botin: m.botin })),
        encontrado: f.encontrado, botin: f.botin_json ?? vacio(), puntos: f.puntos_defensor ?? 0,
        tasa: f.encontrado === 0 ? f.tasa : 0,
      })),
    };
  });

  app.post('/saqueo/informes/visto', sesion, async (req) => {
    await app.pool.query(`UPDATE saqueos SET visto_defensor = true WHERE defensor_id = $1 AND NOT visto_defensor`, [req.jugadorId]);
    return { ok: true };
  });

  app.get('/saqueo/historial', sesion, async (req) => {
    const fila = (f, puntos) => ({
      id: String(f.id), rival: f.rival ?? (f.prueba ? 'Fuerte de prueba' : 'Cuenta borrada'), encontrado: f.encontrado ?? 0,
      marcas: f.marcas ?? 0, botin: f.botin_json ?? vacio(), puntos: puntos ?? 0, creado: f.creado.toISOString(), prueba: f.prueba,
    });
    const hechos = (await app.pool.query(
      `SELECT s.*, d.nombre AS rival FROM saqueos s LEFT JOIN jugadores d ON d.id = s.defensor_id
        WHERE s.atacante_id = $1 AND s.estado = 'terminado' ORDER BY s.id DESC LIMIT 10`, [req.jugadorId])).rows.map(f => fila(f, f.puntos_atacante));
    const recibidos = (await app.pool.query(
      `SELECT s.*, a.nombre AS rival FROM saqueos s LEFT JOIN jugadores a ON a.id = s.atacante_id
        WHERE s.defensor_id = $1 AND s.estado = 'terminado' ORDER BY s.id DESC LIMIT 10`, [req.jugadorId])).rows.map(f => fila(f, f.puntos_defensor));
    return { hechos, recibidos };
  });
}
