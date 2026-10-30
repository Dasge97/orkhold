// Panel de administración de Orkhold (docs/10-panel-admin.md §2). Vue 3 sin compilar.
// La pantalla y la subpestaña van en la URL (#/balance/Saqueo, #/jugadores/12/fuerte...).
const { createApp, reactive, ref, computed, watch, onMounted } = Vue;

// ---------- utilidades ----------

const sesion = reactive({ token: leer('orkhold-panel-token'), correo: leer('orkhold-panel-correo') });
function leer(k) { try { return sessionStorage.getItem(k) || ''; } catch { return ''; } }
function guardar(k, v) { try { v ? sessionStorage.setItem(k, v) : sessionStorage.removeItem(k); } catch { /* sin almacenamiento */ } }

const toasts = reactive([]);
function avisar(texto, error = false) {
  const t = { id: Math.random(), texto, error };
  toasts.push(t);
  setTimeout(() => toasts.splice(toasts.indexOf(t), 1), 4000);
}

async function api(metodo, ruta, cuerpo) {
  const r = await fetch(ruta, {
    method: metodo,
    headers: { ...(cuerpo ? { 'Content-Type': 'application/json' } : {}), ...(sesion.token ? { Authorization: 'Bearer ' + sesion.token } : {}) },
    body: cuerpo ? JSON.stringify(cuerpo) : undefined,
  });
  const datos = await r.json().catch(() => ({}));
  if (r.status === 401 && sesion.token) salir('Tu sesión ha caducado. Vuelve a entrar.');
  if (!r.ok) throw new Error(datos.mensaje || 'No se ha podido completar. Prueba otra vez.');
  return datos;
}

function salir(motivo) {
  sesion.token = ''; sesion.correo = '';
  guardar('orkhold-panel-token', ''); guardar('orkhold-panel-correo', '');
  if (motivo) avisar(motivo, true);
}

const fecha = iso => iso ? new Date(iso).toLocaleString('es-ES', { dateStyle: 'short', timeStyle: 'short' }) : '—';
const recursos = o => Object.entries(o || {}).filter(([, n]) => n).map(([r, n]) => `${n} ${r}`).join(', ') || '—';

// Ruta actual: "#/jugadores/12/fuerte" → ['jugadores', '12', 'fuerte'].
const ruta = ref(location.hash.replace(/^#\/?/, '').split('/').filter(Boolean));
addEventListener('hashchange', () => { ruta.value = location.hash.replace(/^#\/?/, '').split('/').filter(Boolean); });
const ir = (...partes) => { location.hash = '#/' + partes.join('/'); };

// Ventana de confirmación propia.
const confirmacion = reactive({ abierta: false, titulo: '', texto: '', boton: '', peligro: false, campo: '', valor: '', resolver: null });
function confirmar({ titulo, texto, boton, peligro = false, campo = '' }) {
  Object.assign(confirmacion, { abierta: true, titulo, texto, boton, peligro, campo, valor: '' });
  return new Promise(r => { confirmacion.resolver = r; });
}
function cerrarConfirmacion(ok) {
  confirmacion.abierta = false;
  confirmacion.resolver?.(ok ? (confirmacion.campo ? confirmacion.valor : true) : null);
}

// ---------- componentes ----------

const Esqueleto = { props: { filas: { type: Number, default: 5 } }, template: `
  <div class="tarjeta" aria-busy="true" aria-label="Cargando"><div style="display:grid;gap:14px">
    <div v-for="i in filas" :key="i" class="esqueleto" :style="{ width: (60 + (i * 37) % 40) + '%' }"></div></div></div>` };

const Vacio = { props: ['titulo', 'texto'], template: `<div class="vacio tarjeta"><strong>{{ titulo }}</strong><p>{{ texto }}</p><slot /></div>` };

const ErrorCarga = { props: ['texto'], emits: ['reintentar'], template: `
  <div class="aviso aviso--error" role="alert"><div><strong>No se han podido cargar los datos.</strong>
  <p class="muted">{{ texto }}</p></div><button class="boton" style="margin-left:auto" @click="$emit('reintentar')">Reintentar</button></div>` };

/** Carga con los cuatro estados: cargando, error, vacío y con datos. */
function usarCarga(funcion) {
  const estado = reactive({ cargando: true, error: '', datos: null });
  async function cargar() {
    estado.cargando = true; estado.error = '';
    try { estado.datos = await funcion(); } catch (e) { estado.error = e.message; } finally { estado.cargando = false; }
  }
  return { estado, cargar };
}

const Entrada = { setup() {
  const f = reactive({ correo: '', contrasena: '', error: '', enviando: false });
  async function entrar() {
    f.error = ''; f.enviando = true;
    try {
      const r = await api('POST', '/admin/sesion', { correo: f.correo, contrasena: f.contrasena });
      sesion.token = r.token; sesion.correo = r.correo;
      guardar('orkhold-panel-token', r.token); guardar('orkhold-panel-correo', r.correo);
      if (!ruta.value.length) ir('resumen');
    } catch (e) { f.error = e.message; } finally { f.enviando = false; }
  }
  return { f, entrar };
}, template: `
  <main class="entrada"><form class="tarjeta" @submit.prevent="entrar">
    <div><p class="marca">ORK<span>HOLD</span></p><h1 style="margin-top:8px">Panel de administración</h1>
      <p class="muted" style="margin-top:4px">Ajusta el juego, revisa jugadores y saqueos y avisa a todos.</p></div>
    <div class="campo"><label for="correo">Correo</label><input id="correo" v-model="f.correo" type="email" autocomplete="username" required></div>
    <div class="campo"><label for="clave">Contraseña</label><input id="clave" v-model="f.contrasena" type="password" autocomplete="current-password" required>
      <span v-if="f.error" class="error" role="alert">{{ f.error }}</span></div>
    <button class="boton boton--principal" :aria-busy="f.enviando" :disabled="f.enviando">Entrar al panel</button>
  </form></main>` };

const Resumen = { components: { Esqueleto, ErrorCarga }, setup() {
  const { estado, cargar } = usarCarga(() => api('GET', '/admin/resumen'));
  onMounted(cargar);
  return { estado, cargar };
}, template: `
  <div class="cabecera"><div><h1>Resumen</h1><p>Cómo va el juego hoy y si el saqueo necesita ajuste.</p></div>
    <div class="cabecera__accion"><button class="boton" @click="cargar">Actualizar</button></div></div>
  <Esqueleto v-if="estado.cargando && !estado.datos" :filas="4" />
  <ErrorCarga v-else-if="estado.error" :texto="estado.error" @reintentar="cargar" />
  <template v-else-if="estado.datos">
    <div v-if="estado.datos.consejo" class="aviso aviso--aviso"><div><strong>{{ estado.datos.consejo }}</strong>
      <p class="muted">El {{ estado.datos.saqueos_semana.porcentaje }} % de los saqueos de la semana encontró algo; el objetivo es entre
      {{ estado.datos.objetivo.min }} y {{ estado.datos.objetivo.max }} %. <a href="#/balance/Saqueo">Ir al balance del saqueo</a></p></div></div>
    <div class="cifras">
      <div class="tarjeta cifra"><p>Jugadores</p><strong>{{ estado.datos.jugadores }}</strong><small>{{ estado.datos.bloqueados }} bloqueados</small></div>
      <div class="tarjeta cifra"><p>Activos hoy</p><strong>{{ estado.datos.activos_hoy }}</strong></div>
      <div class="tarjeta cifra"><p>En el pueblo ahora</p><strong>{{ estado.datos.en_pueblo ?? '—' }}</strong>
        <small>{{ estado.datos.en_pueblo === null ? 'El servidor de mundo no ha dado señales en 2 minutos' : 'Según el servidor de mundo' }}</small></div>
      <div class="tarjeta cifra"><p>Saqueos hoy</p><strong>{{ estado.datos.saqueos_hoy }}</strong></div>
      <div class="tarjeta cifra"><p>Saqueos con botín (7 días)</p>
        <strong>{{ estado.datos.saqueos_semana.porcentaje === null ? '—' : estado.datos.saqueos_semana.porcentaje + ' %' }}</strong>
        <small>{{ estado.datos.saqueos_semana.con_botin }} de {{ estado.datos.saqueos_semana.total }} · objetivo {{ estado.datos.objetivo.min }}-{{ estado.datos.objetivo.max }} %</small></div>
    </div>
  </template>` };

const Balance = { components: { Esqueleto, ErrorCarga }, props: ['grupo'], setup(props) {
  const { estado, cargar } = usarCarga(() => api('GET', '/admin/balance'));
  const editados = reactive({});
  const guardando = ref(false);
  onMounted(cargar);
  const grupos = computed(() => {
    const g = {};
    for (const v of estado.datos?.valores ?? []) (g[v.grupo] ??= []).push(v);
    return g;
  });
  const actual = computed(() => props.grupo && grupos.value[props.grupo] ? props.grupo : Object.keys(grupos.value)[0]);
  const cambios = computed(() => Object.entries(editados)
    .filter(([c, v]) => v !== '' && Number(v) !== estado.datos.valores.find(x => x.clave === c)?.valor)
    .map(([clave, valor]) => ({ clave, valor: Number(valor) })));
  function valor(f) { return editados[f.clave] ?? f.valor; }
  async function guardarCambios() {
    guardando.value = true;
    try {
      await api('PUT', '/admin/balance', { cambios: cambios.value });
      avisar(`Guardado: ${cambios.value.length} ${cambios.value.length === 1 ? 'número cambiado' : 'números cambiados'}. El juego los usa desde ya.`);
      for (const k of Object.keys(editados)) delete editados[k];
      await cargar();
    } catch (e) { avisar(e.message, true); } finally { guardando.value = false; }
  }
  function defecto(f) { editados[f.clave] = f.por_defecto; }
  return { estado, cargar, grupos, actual, editados, cambios, valor, guardarCambios, guardando, defecto, fecha, ir };
}, template: `
  <div class="cabecera"><div><h1>Balance</h1><p>Todos los números del juego. Un cambio se aplica en cuanto se guarda.</p></div>
    <div class="cabecera__accion">
      <button v-if="cambios.length" class="boton" @click="Object.keys(editados).forEach(k => delete editados[k])">Descartar</button>
      <button class="boton boton--principal" :disabled="!cambios.length || guardando" :aria-busy="guardando" @click="guardarCambios">
        {{ cambios.length ? 'Guardar ' + cambios.length + (cambios.length === 1 ? ' cambio' : ' cambios') : 'Sin cambios' }}</button></div></div>
  <Esqueleto v-if="estado.cargando && !estado.datos" :filas="8" />
  <ErrorCarga v-else-if="estado.error" :texto="estado.error" @reintentar="cargar" />
  <template v-else-if="estado.datos">
    <nav class="subpestanas" aria-label="Grupos">
      <button v-for="(filas, g) in grupos" :key="g" :class="{ on: g === actual }" @click="ir('balance', g)">{{ g }} <span class="cuenta">{{ filas.length }}</span></button>
    </nav>
    <div class="tabla-caja"><table>
      <thead><tr><th>Número</th><th class="num">Valor</th><th class="num">Por defecto</th><th class="num">Rango</th><th>Último cambio</th><th></th></tr></thead>
      <tbody><tr v-for="f in grupos[actual]" :key="f.clave">
        <td><label :for="f.clave">{{ f.descripcion }}</label><span class="descripcion mono">{{ f.clave }}</span></td>
        <td class="num"><input :id="f.clave" type="number" step="any" :min="f.minimo" :max="f.maximo" :value="valor(f)"
          :class="{ cambiado: editados[f.clave] !== undefined && Number(editados[f.clave]) !== f.valor }" @input="editados[f.clave] = $event.target.value"></td>
        <td class="num">{{ f.por_defecto }}</td>
        <td class="num muted">{{ f.minimo }} – {{ f.maximo }}</td>
        <td class="muted">{{ f.actualizado_por ? fecha(f.actualizado) + ' · ' + f.actualizado_por : 'Sin cambios' }}</td>
        <td><button v-if="valor(f) != f.por_defecto" class="boton boton--texto" @click="defecto(f)">Volver al defecto</button></td>
      </tr></tbody></table></div>
  </template>` };

const Historial = { components: { Esqueleto, ErrorCarga, Vacio }, setup() {
  const { estado, cargar } = usarCarga(() => api('GET', '/admin/balance/historial'));
  onMounted(cargar);
  async function deshacer(c) {
    const ok = await confirmar({ titulo: 'Deshacer el cambio', texto: `${c.clave} vuelve a ${c.anterior}. El juego lo usa desde ya.`, boton: 'Deshacer' });
    if (!ok) return;
    try { await api('POST', '/admin/balance/deshacer', { id: c.id }); avisar(`${c.clave} vuelve a ${c.anterior}.`); cargar(); }
    catch (e) { avisar(e.message, true); }
  }
  return { estado, cargar, deshacer, fecha };
}, template: `
  <div class="cabecera"><div><h1>Historial</h1><p>Quién cambió cada número y cuándo. Cualquier cambio se puede deshacer.</p></div></div>
  <Esqueleto v-if="estado.cargando && !estado.datos" />
  <ErrorCarga v-else-if="estado.error" :texto="estado.error" @reintentar="cargar" />
  <Vacio v-else-if="!estado.datos.cambios.length" titulo="Aún no se ha cambiado nada" texto="El juego usa los valores por defecto. Los cambios del balance aparecerán aquí.">
    <a class="boton" href="#/balance" style="margin-top:16px">Ir al balance</a></Vacio>
  <div v-else class="tabla-caja"><table>
    <thead><tr><th>Cuándo</th><th>Número</th><th class="num">Antes</th><th class="num">Después</th><th>Quién</th><th></th></tr></thead>
    <tbody><tr v-for="c in estado.datos.cambios" :key="c.id">
      <td>{{ fecha(c.creado) }}</td><td class="mono">{{ c.clave }}</td><td class="num">{{ c.anterior }}</td><td class="num">{{ c.nuevo }}</td>
      <td class="muted">{{ c.quien }}</td><td><button class="boton boton--texto" @click="deshacer(c)">Deshacer</button></td></tr></tbody></table></div>` };

const Rejilla = { props: ['diseno'], setup(props) {
  const ANCHO = 16;
  const celdas = computed(() => {
    const porCasilla = new Map((props.diseno?.piezas ?? []).map(p => [`${p.x},${p.z}`, p]));
    const lista = [];
    for (let fila = 0; fila < ANCHO; fila++) for (let x = 0; x < ANCHO; x++) {
      const z = ANCHO - 1 - fila;
      const p = porCasilla.get(`${x},${z}`);
      let clase = 'casilla', texto = '', titulo = `(${x},${z})`;
      if (x === 0 || z === 0 || x === ANCHO - 1 || z === ANCHO - 1) clase += ' casilla--borde';
      if (x === 8 && z === 7) { clase += ' casilla--esc'; texto = 'C'; titulo += ' cofre central: ' + recursos(props.diseno?.central); }
      if (p) {
        const esc = ['cofre', 'barril', 'enterrado', 'doble_fondo'].includes(p.tipo);
        const def = /defensor|jefe/.test(p.tipo);
        clase += esc ? ' casilla--esc' : def ? ' casilla--def' : p.tipo === 'trampa' ? '' : ' casilla--muro';
        texto = esc ? p.tipo[0].toUpperCase() : def ? 'D' : p.tipo === 'trampa' ? 'T' : '';
        titulo += ' ' + p.tipo + (esc ? ': ' + recursos(p.contenido) : '');
      }
      lista.push({ clase, texto, titulo });
    }
    return lista;
  });
  return { celdas };
}, template: `<div><div class="rejilla" style="grid-template-columns:repeat(16,1fr);grid-template-rows:repeat(16,1fr)" role="img" aria-label="Plano del fuerte">
  <div v-for="(c, i) in celdas" :key="i" :class="c.clase" :title="c.titulo">{{ c.texto }}</div></div>
  <div class="leyenda"><span>C cofre central</span><span>c cofre · b barril · e enterrado</span><span>D defensor</span><span>T trampa</span><span>gris: muro</span></div></div>` };

const Jugadores = { components: { Esqueleto, ErrorCarga, Vacio }, setup() {
  const q = ref('');
  const { estado, cargar } = usarCarga(() => api('GET', '/admin/jugadores?q=' + encodeURIComponent(q.value)));
  onMounted(cargar);
  let espera;
  watch(q, () => { clearTimeout(espera); espera = setTimeout(cargar, 300); });
  return { q, estado, cargar, fecha, ir };
}, template: `
  <div class="cabecera"><div><h1>Jugadores</h1><p>Busca por nombre de personaje o correo. Toca una fila para ver su ficha.</p></div>
    <div class="cabecera__accion"><div class="campo"><label for="buscar" class="muted" style="font-weight:400">Buscar</label>
      <input id="buscar" v-model="q" type="search" placeholder="Nombre o correo" style="width:260px"></div></div></div>
  <Esqueleto v-if="estado.cargando && !estado.datos" />
  <ErrorCarga v-else-if="estado.error" :texto="estado.error" @reintentar="cargar" />
  <Vacio v-else-if="!estado.datos.jugadores.length" :titulo="q ? 'Nadie coincide con «' + q + '»' : 'Todavía no hay jugadores'"
    :texto="q ? 'Prueba con otra parte del nombre o del correo.' : 'Cuando alguien abra el juego aparecerá aquí.'" />
  <div v-else class="tabla-caja"><table>
    <thead><tr><th>Personaje</th><th>Correo</th><th class="num">Puntuación</th><th>Último acceso</th><th>Estado</th></tr></thead>
    <tbody><tr v-for="j in estado.datos.jugadores" :key="j.id" class="clicable" tabindex="0" @click="ir('jugadores', j.id)" @keydown.enter="ir('jugadores', j.id)">
      <td>{{ j.nombre || 'Sin personaje' }}</td><td class="muted">{{ j.correo || 'Invitada' }}</td><td class="num">{{ j.puntuacion }}</td>
      <td>{{ fecha(j.ultimo_acceso) }}</td>
      <td><span v-if="j.bloqueado" class="insignia insignia--error">Bloqueado</span><span v-else class="insignia insignia--ok">Activo</span></td>
    </tr></tbody></table></div>` };

const FichaJugador = { components: { Esqueleto, ErrorCarga, Rejilla }, props: ['id', 'sub'], setup(props) {
  const { estado, cargar } = usarCarga(() => api('GET', '/admin/jugadores/' + props.id));
  onMounted(cargar);
  const sub = computed(() => props.sub || 'resumen');
  async function renombrar() {
    const nombre = await confirmar({ titulo: 'Cambiar el nombre', texto: 'De 3 a 16 letras. El jugador lo verá la próxima vez que entre.', boton: 'Cambiar nombre', campo: 'Nombre nuevo' });
    if (!nombre) return;
    try { await api('PUT', `/admin/jugadores/${props.id}/nombre`, { nombre }); avisar('Nombre cambiado.'); cargar(); } catch (e) { avisar(e.message, true); }
  }
  async function bloquear(si) {
    let motivo = '';
    if (si) {
      motivo = await confirmar({ titulo: 'Bloquear la cuenta', texto: 'No podrá entrar al juego ni a la web hasta que la desbloquees.', boton: 'Bloquear', peligro: true, campo: 'Motivo' });
      if (!motivo) return;
    }
    try { await api('POST', `/admin/jugadores/${props.id}/bloquear`, { bloquear: si, motivo }); avisar(si ? 'Cuenta bloqueada.' : 'Cuenta desbloqueada.'); cargar(); }
    catch (e) { avisar(e.message, true); }
  }
  return { estado, cargar, sub, renombrar, bloquear, fecha, recursos, ir };
}, template: `
  <Esqueleto v-if="estado.cargando && !estado.datos" />
  <ErrorCarga v-else-if="estado.error" :texto="estado.error" @reintentar="cargar" />
  <template v-else-if="estado.datos">
    <div class="cabecera"><div><h1>{{ estado.datos.jugador.nombre || 'Sin personaje' }}</h1>
      <p>{{ estado.datos.jugador.correo || 'Cuenta invitada' }} · {{ estado.datos.jugador.puntuacion }} puntos · desde {{ fecha(estado.datos.jugador.creado) }}</p></div>
      <div class="cabecera__accion"><button class="boton" @click="renombrar">Cambiar nombre</button>
        <button v-if="estado.datos.jugador.bloqueado" class="boton" @click="bloquear(false)">Desbloquear</button>
        <button v-else class="boton boton--peligro" @click="bloquear(true)">Bloquear</button></div></div>
    <div v-if="estado.datos.jugador.bloqueado" class="aviso aviso--error"><div><strong>Cuenta bloqueada</strong><p class="muted">{{ estado.datos.jugador.bloqueado_motivo }}</p></div></div>
    <nav class="subpestanas" aria-label="Secciones del jugador">
      <button :class="{ on: sub === 'resumen' }" @click="ir('jugadores', id)">Resumen</button>
      <button :class="{ on: sub === 'fuerte' }" @click="ir('jugadores', id, 'fuerte')">Fuerte</button>
      <button :class="{ on: sub === 'saqueos' }" @click="ir('jugadores', id, 'saqueos')">Saqueos
        <span class="cuenta">{{ estado.datos.saqueos.hechos.length + estado.datos.saqueos.recibidos.length }}</span></button>
    </nav>
    <div v-if="sub === 'resumen'" class="dos-columnas">
      <div class="tarjeta"><h2 style="margin-bottom:16px">Cuenta</h2><dl class="lista-datos">
        <dt>Tipo</dt><dd>{{ estado.datos.jugador.correo ? (estado.datos.jugador.verificado ? 'Registrada y verificada' : 'Registrada sin verificar') : 'Invitada' }}</dd>
        <dt>Modelo</dt><dd>{{ estado.datos.jugador.modelo || '—' }}</dd>
        <dt>Último acceso</dt><dd>{{ fecha(estado.datos.jugador.ultimo_acceso) }}</dd></dl></div>
      <div class="tarjeta"><h2 style="margin-bottom:16px">Inventario</h2><dl class="lista-datos">
        <template v-for="(n, r) in estado.datos.inventario" :key="r"><dt>{{ r }}</dt><dd>{{ n }}</dd></template></dl></div>
    </div>
    <div v-else-if="sub === 'fuerte'" class="tarjeta"><h2 style="margin-bottom:16px">Fuerte</h2>
      <p class="muted" style="margin-bottom:16px">Pasa el ratón por una casilla para ver qué hay y cuánto guarda.</p>
      <Rejilla :diseno="estado.datos.fuerte" /></div>
    <div v-else class="dos-columnas">
      <div v-for="(lista, titulo) in { 'Saqueos hechos': estado.datos.saqueos.hechos, 'Saqueos recibidos': estado.datos.saqueos.recibidos }" :key="titulo">
        <h2 style="margin-bottom:12px">{{ titulo }}</h2>
        <div v-if="!lista.length" class="vacio tarjeta">Ninguno todavía.</div>
        <div v-else class="tabla-caja"><table><thead><tr><th>Cuándo</th><th>Rival</th><th>Botín</th><th class="num">Puntos</th></tr></thead>
          <tbody><tr v-for="s in lista" :key="s.id"><td>{{ fecha(s.creado) }}</td><td>{{ s.rival }}</td><td>{{ recursos(s.botin) }}</td>
            <td class="num">{{ s.puntos > 0 ? '+' + s.puntos : s.puntos }}</td></tr></tbody></table></div>
      </div>
    </div>
  </template>` };

const Saqueos = { components: { Esqueleto, ErrorCarga, Vacio }, props: ['sub'], setup(props) {
  const sospechosos = computed(() => props.sub === 'sospechosos');
  const { estado, cargar } = usarCarga(() => api('GET', '/admin/saqueos?sospechosos=' + (sospechosos.value ? 1 : 0)));
  onMounted(cargar);
  watch(sospechosos, cargar);
  return { estado, cargar, sospechosos, fecha, recursos, ir };
}, template: `
  <div class="cabecera"><div><h1>Saqueos</h1><p>Los últimos 100. "Sospechosos" junta los que tienen algo raro.</p></div></div>
  <nav class="subpestanas" aria-label="Filtro">
    <button :class="{ on: !sospechosos }" @click="ir('saqueos')">Todos</button>
    <button :class="{ on: sospechosos }" @click="ir('saqueos', 'sospechosos')">Sospechosos</button></nav>
  <Esqueleto v-if="estado.cargando && !estado.datos" />
  <ErrorCarga v-else-if="estado.error" :texto="estado.error" @reintentar="cargar" />
  <Vacio v-else-if="!estado.datos.saqueos.length" :titulo="sospechosos ? 'Ningún saqueo sospechoso' : 'Aún no hay saqueos'"
    :texto="sospechosos ? 'Ningún saqueo reciente tiene aciertos imposibles ni entradas descartadas.' : 'Aparecerán aquí en cuanto alguien saquee un fuerte.'" />
  <div v-else class="tabla-caja"><table>
    <thead><tr><th>Cuándo</th><th>Atacante</th><th>Defensor</th><th class="num">Aciertos</th><th>Botín</th><th class="num">Exploración</th><th>Revisar</th></tr></thead>
    <tbody><tr v-for="s in estado.datos.saqueos" :key="s.id">
      <td>{{ fecha(s.creado) }}</td><td>{{ s.atacante }}</td><td>{{ s.defensor }}</td><td class="num">{{ s.encontrado }} / {{ s.marcas }}</td>
      <td>{{ recursos(s.botin) }}</td><td class="num">{{ s.segundos }} s</td>
      <td><span v-for="m in s.sospechoso" :key="m" class="insignia insignia--aviso" style="margin-right:4px">{{ m }}</span>
        <span v-if="!s.sospechoso.length" class="muted">—</span></td></tr></tbody></table></div>` };

const Eventos = { components: { Esqueleto, ErrorCarga, Vacio }, setup() {
  const { estado, cargar } = usarCarga(() => api('GET', '/admin/eventos'));
  onMounted(cargar);
  const f = reactive({ tipo: '', inicio: '', fin: '', parametros: '{}', error: '', enviando: false });
  async function crear() {
    f.error = '';
    let parametros;
    try { parametros = JSON.parse(f.parametros || '{}'); } catch { f.error = 'Los parámetros tienen que ser JSON, por ejemplo {"botin": 2}.'; return; }
    f.enviando = true;
    try {
      await api('POST', '/admin/eventos', { tipo: f.tipo, inicio: new Date(f.inicio).toISOString(), fin: new Date(f.fin).toISOString(), parametros });
      avisar('Evento programado.'); Object.assign(f, { tipo: '', inicio: '', fin: '', parametros: '{}' }); cargar();
    } catch (e) { f.error = e.message; } finally { f.enviando = false; }
  }
  async function borrar(e) {
    if (!(await confirmar({ titulo: 'Borrar el evento', texto: `${e.tipo}, del ${fecha(e.inicio)} al ${fecha(e.fin)}.`, boton: 'Borrar', peligro: true }))) return;
    try { await api('DELETE', '/admin/eventos/' + e.id); avisar('Evento borrado.'); cargar(); } catch (er) { avisar(er.message, true); }
  }
  return { estado, cargar, f, crear, borrar, fecha };
}, template: `
  <div class="cabecera"><div><h1>Eventos</h1><p>Programa eventos del pueblo con fecha. El juego los usará cuando estén diseñados (docs/12-eventos.md).</p></div></div>
  <form class="tarjeta formulario formulario--fila" style="margin-bottom:24px" @submit.prevent="crear">
    <div class="campo"><label for="ev-tipo">Tipo</label><input id="ev-tipo" v-model="f.tipo" required placeholder="luna_roja"></div>
    <div class="campo"><label for="ev-ini">Empieza</label><input id="ev-ini" v-model="f.inicio" type="datetime-local" required></div>
    <div class="campo"><label for="ev-fin">Termina</label><input id="ev-fin" v-model="f.fin" type="datetime-local" required></div>
    <div class="campo"><label for="ev-par">Parámetros</label><input id="ev-par" v-model="f.parametros" class="mono"><small>JSON, por ejemplo {"botin": 2}</small></div>
    <div><button class="boton boton--principal" :aria-busy="f.enviando" :disabled="f.enviando">Programar evento</button></div>
    <p v-if="f.error" class="campo" style="grid-column:1/-1"><span class="error" role="alert">{{ f.error }}</span></p>
  </form>
  <Esqueleto v-if="estado.cargando && !estado.datos" :filas="3" />
  <ErrorCarga v-else-if="estado.error" :texto="estado.error" @reintentar="cargar" />
  <Vacio v-else-if="!estado.datos.eventos.length" titulo="No hay eventos programados" texto="Programa el primero con el formulario de arriba." />
  <div v-else class="tabla-caja"><table><thead><tr><th>Tipo</th><th>Empieza</th><th>Termina</th><th>Parámetros</th><th>Creado por</th><th></th></tr></thead>
    <tbody><tr v-for="e in estado.datos.eventos" :key="e.id"><td>{{ e.tipo }}</td><td>{{ fecha(e.inicio) }}</td><td>{{ fecha(e.fin) }}</td>
      <td class="mono">{{ JSON.stringify(e.parametros) }}</td><td class="muted">{{ e.creado_por }}</td>
      <td><button class="boton boton--texto" @click="borrar(e)">Borrar</button></td></tr></tbody></table></div>` };

const Servidor = { components: { Esqueleto, ErrorCarga }, setup() {
  const { estado, cargar } = usarCarga(() => api('GET', '/admin/servidor'));
  const f = reactive({ version_minima: 0, mantenimiento: '', enviando: false });
  onMounted(async () => { await cargar(); if (estado.datos) Object.assign(f, { version_minima: estado.datos.version_minima, mantenimiento: estado.datos.mantenimiento }); });
  async function guardarServidor() {
    f.enviando = true;
    try { await api('PUT', '/admin/servidor', { version_minima: Number(f.version_minima), mantenimiento: f.mantenimiento });
      avisar(f.mantenimiento ? 'Guardado. El juego no deja entrar mientras haya mensaje de mantenimiento.' : 'Guardado.'); cargar(); }
    catch (e) { avisar(e.message, true); } finally { f.enviando = false; }
  }
  return { estado, cargar, f, guardarServidor, fecha };
}, template: `
  <div class="cabecera"><div><h1>Servidor</h1><p>Versión mínima del juego y modo mantenimiento.</p></div></div>
  <Esqueleto v-if="estado.cargando && !estado.datos" :filas="3" />
  <ErrorCarga v-else-if="estado.error" :texto="estado.error" @reintentar="cargar" />
  <div v-else class="dos-columnas">
    <form class="tarjeta formulario" @submit.prevent="guardarServidor">
      <div class="campo"><label for="sv-ver">Versión mínima del juego</label><input id="sv-ver" v-model="f.version_minima" type="number" min="0">
        <small>Los móviles con una versión anterior piden actualizar. 0 deja entrar a todos.</small></div>
      <div class="campo"><label for="sv-man">Mensaje de mantenimiento</label><textarea id="sv-man" v-model="f.mantenimiento" maxlength="300"></textarea>
        <small>Si hay mensaje, el juego lo enseña y no deja entrar. Vacío para abrir.</small></div>
      <div><button class="boton boton--principal" :aria-busy="f.enviando" :disabled="f.enviando">Guardar</button></div>
    </form>
    <div class="tarjeta"><h2 style="margin-bottom:16px">Servidor de mundo</h2><dl class="lista-datos">
      <dt>Jugadores en el pueblo</dt><dd>{{ estado.datos.jugadores_pueblo }}</dd>
      <dt>Última señal</dt><dd>{{ fecha(estado.datos.latido) }}</dd></dl></div>
  </div>` };

const Avisos = { components: { Esqueleto, ErrorCarga, Vacio }, setup() {
  const { estado, cargar } = usarCarga(() => api('GET', '/admin/avisos'));
  onMounted(cargar);
  const f = reactive({ titulo: '', texto: '', enviando: false });
  async function enviar() {
    if (!(await confirmar({ titulo: 'Mandar el aviso', texto: `Todos los jugadores verán «${f.titulo}» la próxima vez que abran el juego.`, boton: 'Mandar a todos' }))) return;
    f.enviando = true;
    try { await api('POST', '/admin/avisos', { titulo: f.titulo, texto: f.texto }); avisar('Aviso mandado.'); Object.assign(f, { titulo: '', texto: '' }); cargar(); }
    catch (e) { avisar(e.message, true); } finally { f.enviando = false; }
  }
  async function retirar(a) {
    try { await api('POST', `/admin/avisos/${a.id}/retirar`); avisar('Aviso retirado. Ya no se enseña.'); cargar(); } catch (e) { avisar(e.message, true); }
  }
  return { estado, cargar, f, enviar, retirar, fecha };
}, template: `
  <div class="cabecera"><div><h1>Avisos</h1><p>Un mensaje para todos los jugadores. El juego lo enseña una vez al arrancar.</p></div></div>
  <div class="dos-columnas">
    <form class="tarjeta formulario" @submit.prevent="enviar">
      <div class="campo"><label for="av-tit">Título</label><input id="av-tit" v-model="f.titulo" maxlength="80" required></div>
      <div class="campo"><label for="av-tex">Texto</label><textarea id="av-tex" v-model="f.texto" maxlength="500" required></textarea></div>
      <div><button class="boton boton--principal" :aria-busy="f.enviando" :disabled="f.enviando">Mandar aviso</button></div>
    </form>
    <div>
      <Esqueleto v-if="estado.cargando && !estado.datos" :filas="3" />
      <ErrorCarga v-else-if="estado.error" :texto="estado.error" @reintentar="cargar" />
      <Vacio v-else-if="!estado.datos.avisos.length" titulo="Ningún aviso mandado" texto="Escribe el primero a la izquierda." />
      <div v-else class="tabla-caja"><table><thead><tr><th>Aviso</th><th>Cuándo</th><th>Estado</th><th></th></tr></thead>
        <tbody><tr v-for="a in estado.datos.avisos" :key="a.id"><td><strong>{{ a.titulo }}</strong><span class="descripcion">{{ a.texto }}</span></td>
          <td>{{ fecha(a.creado) }}</td><td><span :class="['insignia', a.activo ? 'insignia--ok' : '']">{{ a.activo ? 'Se enseña' : 'Retirado' }}</span></td>
          <td><button v-if="a.activo" class="boton boton--texto" @click="retirar(a)">Retirar</button></td></tr></tbody></table></div>
    </div>
  </div>` };

// ---------- aplicación ----------

const SECCIONES = [
  ['resumen', 'Resumen'], ['balance', 'Balance'], ['historial', 'Historial'], ['jugadores', 'Jugadores'],
  ['saqueos', 'Saqueos'], ['eventos', 'Eventos'], ['servidor', 'Servidor'], ['avisos', 'Avisos'],
];

createApp({
  components: { Entrada, Resumen, Balance, Historial, Jugadores, FichaJugador, Saqueos, Eventos, Servidor, Avisos },
  setup() {
    const seccion = computed(() => SECCIONES.find(s => s[0] === ruta.value[0]) ? ruta.value[0] : 'resumen');
    const nombre = computed(() => SECCIONES.find(s => s[0] === seccion.value)[1]);
    const ficha = computed(() => seccion.value === 'jugadores' && ruta.value[1]);
    const tema = ref((() => { try { return localStorage.getItem('orkhold-panel-tema') || 'sistema'; } catch { return 'sistema'; } })());
    watch(tema, t => {
      if (t === 'sistema') delete document.documentElement.dataset.theme; else document.documentElement.dataset.theme = t;
      try { localStorage.setItem('orkhold-panel-tema', t); } catch { /* sin almacenamiento */ }
    });
    return { sesion, ruta, seccion, nombre, ficha, SECCIONES, tema, toasts, confirmacion, cerrarConfirmacion, salir };
  },
  template: `
    <Entrada v-if="!sesion.token" />
    <template v-else>
      <header class="barra">
        <div class="barra__fila">
          <nav class="migas" aria-label="Estás en">
            <a href="#/resumen" class="marca">ORK<span>HOLD</span></a><span class="migas__sep">›</span>
            <template v-if="ficha"><a href="#/jugadores">Jugadores</a><span class="migas__sep">›</span><strong>Ficha</strong></template>
            <template v-else-if="seccion === 'balance' && ruta[1]"><a href="#/balance">Balance</a><span class="migas__sep">›</span><strong>{{ decodeURIComponent(ruta[1]) }}</strong></template>
            <strong v-else>{{ nombre }}</strong>
          </nav>
          <div class="barra__acciones">
            <label class="muted" for="tema" style="font-size:14px">Tema</label>
            <select id="tema" v-model="tema" style="width:auto"><option value="sistema">Sistema</option><option value="light">Claro</option><option value="dark">Oscuro</option></select>
            <button class="boton" @click="salir()" :title="sesion.correo">Salir</button>
          </div>
        </div>
        <nav class="pestanas" aria-label="Secciones">
          <a v-for="[id, texto] in SECCIONES" :key="id" :href="'#/' + id" :class="{ on: id === seccion }" :aria-current="id === seccion ? 'page' : null">{{ texto }}</a>
        </nav>
      </header>
      <main class="contenido">
        <Resumen v-if="seccion === 'resumen'" />
        <Balance v-else-if="seccion === 'balance'" :grupo="ruta[1] && decodeURIComponent(ruta[1])" />
        <Historial v-else-if="seccion === 'historial'" />
        <FichaJugador v-else-if="ficha" :key="ruta[1]" :id="ruta[1]" :sub="ruta[2]" />
        <Jugadores v-else-if="seccion === 'jugadores'" />
        <Saqueos v-else-if="seccion === 'saqueos'" :sub="ruta[1]" />
        <Eventos v-else-if="seccion === 'eventos'" />
        <Servidor v-else-if="seccion === 'servidor'" />
        <Avisos v-else-if="seccion === 'avisos'" />
      </main>
    </template>
    <div v-if="confirmacion.abierta" class="velo" @click.self="cerrarConfirmacion(false)" @keydown.esc="cerrarConfirmacion(false)">
      <form class="ventana" role="dialog" aria-modal="true" :aria-label="confirmacion.titulo" @submit.prevent="cerrarConfirmacion(true)">
        <h2>{{ confirmacion.titulo }}</h2><p class="muted">{{ confirmacion.texto }}</p>
        <div v-if="confirmacion.campo" class="campo"><label for="conf-campo">{{ confirmacion.campo }}</label>
          <input id="conf-campo" v-model="confirmacion.valor" required autofocus></div>
        <div class="ventana__acciones"><button type="button" class="boton" @click="cerrarConfirmacion(false)">Cancelar</button>
          <button :class="['boton', confirmacion.peligro ? 'boton--peligro' : 'boton--principal']">{{ confirmacion.boton }}</button></div>
      </form>
    </div>
    <div class="toasts" aria-live="polite"><div v-for="t in toasts" :key="t.id" :class="['toast', t.error && 'toast--error']">{{ t.texto }}</div></div>`,
}).mount('#app');
