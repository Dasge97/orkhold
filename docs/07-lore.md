# 07 · Lore y personajes

Fecha: 2026-10-30. Todo es **propuesta** hasta que Daniel lo apruebe. El objetivo del lore no es
contar una historia larga: es dar una razón sencilla a cada mecánica que ya existe (reaparecer en la
fogata, comprar orcos como defensores, asaltar a otros jugadores sin que nadie pierda su fuerte).

## 1. El mundo en diez líneas

Orkhold es un valle del norte, cerrado por montañas, con un río que lo cruza. Hace cien años los orcos
levantaron allí su fortaleza mayor, **la Orkhold** ("el Baluarte Orco" en su lengua). El último rey
orco murió sin heredero, los clanes se mataron entre sí por el trono y la fortaleza quedó abandonada.
Lo que queda hoy: ruinas por todo el valle, bandas de orcos sueltos sin rey, y bajo las ruinas las
**catacumbas**, donde están enterrados los reyes orcos con su tesoro.

Hace diez años empezaron a llegar colonos del sur, atraídos por el hierro del valle y por los rumores
del tesoro. Fundaron un pueblo sobre el puente del río: **Vado Gris**. Los colonos son los jugadores.

En la plaza del pueblo arde **la Brasa**: un fuego que los orcos encendieron al fundar la fortaleza
y que nunca se ha apagado. Quien cae en el valle despierta junto a la Brasa. Nadie sabe por qué.
Por eso en Orkhold nadie muere del todo, y por eso nadie se toma el asalto a un fuerte como una guerra:
es un juego peligroso, no una muerte.

## 2. Cómo explica el lore cada mecánica

| Mecánica que ya existe | Explicación |
|---|---|
| Reaparecer en la fogata y curarse en ella | La Brasa devuelve al valle a quien cae y cierra sus heridas. |
| Los orcos atacan en el pueblo | Son bandas sin rey que roban a los colonos. |
| Comprar orcos defensores con monedas | "Un orco sin rey sirve a quien le paga." Las monedas son las viejas monedas orcas, lo único que respetan. |
| Esqueleto defensor con monedas y mejora con hueso | Los huesos de las catacumbas guardan algo de la voluntad de los viejos guardias. Con hueso suficiente, vuelven a montar guardia. |
| El orco jefe se une a ti al vencerle | Costumbre orca: el que vence manda. Urzog jura lealtad a quien le gana la primera vez. |
| Cada colono tiene un fuerte y un cofre | Cada colono guarda lo que saca del valle en su propio cofre, dentro de su cerca. El tesoro atrae ladrones. |
| Asaltar a otro jugador está permitido | **La Ley del Vado**: quien toque el cofre de otro antes de que la Brasa lo devuelva se lleva una parte. El asaltado no pierde el fuerte: la Brasa levanta de nuevo los muros caídos. Se firmó para que los colonos no se mataran de verdad por el tesoro. |
| El fuerte está en un lateral del pueblo y solo lo ves tú | Cada colono tiene su parcela en las tierras del este. Al asaltar, el atacante "sueña" el fuerte del otro por la Brasa; por eso no pisa el de verdad. (Esto da también la excusa para repeticiones futuras: la Brasa recuerda.) |
| Ranking en el tablón | El tablón de la plaza lleva la cuenta de la Ley del Vado: quién ha robado a quién. |
| Más puertas de mazmorra con el tiempo | El cartógrafo del pueblo va encontrando entradas nuevas a las ruinas. |
| Madera, piedra, hierro, hueso | Bosque del valle, piedras de las ruinas, armas de los orcos, catacumbas. |

## 3. Tono

Serio pero ligero. Sin sangre, sin tragedia. Los personajes hablan corto y con algo de humor seco.
Textos de juego de una o dos frases, nunca párrafos: se leen en un móvil en horizontal.

## 4. Lugares del valle

| Lugar | Qué es | En el juego |
|---|---|---|
| Vado Gris | El pueblo, sobre el puente del río | El mapa general (`pueblo.txt`) |
| La Brasa | El fuego eterno de la plaza | Fogata: aparición y curación |
| Tierras del Este | Parcelas de los colonos | Hueco del fuerte |
| Ruinas de la Orkhold | La fortaleza orca caída, al norte | La puerta de la mazmorra está en sus ruinas |
| Las Catacumbas | Tumbas de los reyes orcos | Mazmorra 1 (`catacumbas.txt`) |
| El Bosque Bajo | Bosque en los bordes del valle | Árboles para madera |
| Futuro: la Forja Hundida, la Torre del Vigía, el Pozo de los Clanes | Mazmorras 2, 3, 4 | Puertas nuevas en el pueblo |

## 5. Personajes (NPC)

Todos usan modelos que ya están en el proyecto. Los colonos, un modelo de `MiniCharacters` que no
esté entre los 4 que elige el jugador. Cada NPC tiene **un sitio fijo** y **una función de juego**;
no hay NPC que solo hable.

### 5.1 Maela, la Guardiana de la Brasa
- **Modelo:** `character-female-e`, color pardo. Junto a la fogata.
- **Función:** tutorial (sustituye o acompaña a los cinco carteles) y curación. Es la primera que ves.
- **Quién es:** la colona más vieja. Lleva veinte años vigilando que la Brasa no se apague. Habla poco.
- **Frases:** "Si caes, vuelves aquí. No es un favor, es el valle." · "Toca el fuego. Lo demás lo hace él."

### 5.2 Tobin, el Tablonero
- **Modelo:** `character-male-b`. Junto al tablón de la plaza.
- **Función:** ranking e historial de asaltos. Explica la Ley del Vado la primera vez.
- **Quién es:** escribano del pueblo, lleva la cuenta de quién ha robado a quién. Le encanta su trabajo.
- **Frases:** "¿Te han asaltado? Apúntalo. ¿Has asaltado? También. Yo no juzgo, yo apunto." · "Arriba del tablón están los que roban mejor. Abajo, los que construyen peor."

### 5.3 Gruk Mediopaga, el orco mercenario
- **Modelo:** `character-orc`, color gris, con `banner`. En el borde este del pueblo, a la entrada de las Tierras del Este.
- **Función:** vende orcos y esqueletos defensores (hoy se compran en el modo construir; Gruk es la cara de esa compra).
- **Quién es:** desertor del último clan. Cobra en monedas viejas. "Mediopaga" porque dice que lo que cobra es la mitad de lo que vale.
- **Frases:** "Un orco sin rey sirve a quien paga. Yo soy el que cobra." · "Te los vendo fieles. Lo que hagan en tu fuerte ya es cosa tuya."

### 5.4 Ferra, la herrera
- **Modelo:** `character-female-b`, delantal oscuro. Casa con `barrel` y `table` delante.
- **Función:** hierro → trampas. En el futuro, fabrica las **herramientas de asalto** (ver `08-formato-y-pvp.md`).
- **Quién es:** vino al valle por el hierro. Odia a los orcos y admira sus armas.
- **Frases:** "Hierro orco. Mal forjado, bien templado. Tráeme más." · "Una trampa no es una traición. Es una advertencia que llega tarde."

### 5.5 Ansel, el cartógrafo
- **Modelo:** `character-male-e`, con `table` y papeles. Cerca de la puerta de la mazmorra, al norte.
- **Función:** mapas y mazmorras. Cuando se añade una mazmorra nueva al juego, "Ansel ha encontrado una entrada". Vende el mapa de la mazmorra (ver trampas y salas antes de entrar) por monedas.
- **Quién es:** quiere dibujar toda la Orkhold antes de morirse. Lleva diez años y le falta casi todo.
- **Frases:** "Las ruinas tienen más puertas que paredes. Yo las voy contando." · "Baja con mapa o baja dos veces."

### 5.6 Rook, vigilante de las Tierras del Este
- **Modelo:** `character-male-d`, con `weapon-spear`. En la entrada del hueco del fuerte.
- **Función:** abre el modo construir y explica las piezas y sus límites. Da los avisos de "te han asaltado".
- **Quién es:** antiguo soldado. Vigila las parcelas pero, por la Ley del Vado, no puede impedir un asalto. Le fastidia.
- **Frases:** "Yo vigilo. No defiendo. Para defender, construye." · "Anoche entraron en tu parcela. Tocaron el cofre. Lo siento."

### 5.7 Urzog, Guardián de la Cripta (jefe de la mazmorra 1)
- **Modelo:** `character-orc` al 150 %, color oscuro.
- **Función:** jefe de las catacumbas. Al vencerle la primera vez, se une como "Orco jefe defensor".
- **Quién es:** el último orco que juró guardar la tumba del último rey. Lleva cien años abajo. Cuando le ganas, considera que el rey ya tiene sucesor: tú.
- **Frases:** al entrar: "Cien años. Nadie pasó." · al perder: "Eres más fuerte. Entonces mandas. Así es la ley." · en tu fuerte: "Guardé a un rey muerto. Guardaré a uno vivo."

### 5.8 Enemigos con nombre de banda (no NPC, solo sabor)
- Los orcos sueltos del pueblo son **la banda del Colmillo Roto**.
- Los arqueros, **los Cazadores del Vado**.
- Los esqueletos de las catacumbas, **la Guardia del Rey Muerto**.
Estos nombres salen en el texto de carga ("Consejo: los Cazadores del Vado huyen si te acercas").

## 6. Textos de carga (pantalla de carga, uno al azar)

- "La Brasa lleva cien años encendida. Nadie la vigila. Nadie hace falta."
- "Un orco sin rey sirve a quien le paga. Un orco con rey, también."
- "La Ley del Vado: si tocan tu cofre, pierdes una parte. Si tocas el suyo, ganas una parte. Nadie pierde el fuerte."
- "Los muros de piedra aguantan el doble. También cuestan el doble de golpes."
- "Las trampas no se ven. Pisa donde ya has pisado."
- "Urzog lleva cien años abajo. No tiene prisa. Tú sí."
- "Ansel dice que a las ruinas les faltan puertas por encontrar. Ansel dice muchas cosas."

## 7. Qué cambia en el juego actual (aprobado el 30-10-2026)

Pequeño y en orden:
1. Textos de carga: la lista del §6 en vez de los consejos actuales (o mezclados).
2. Los cinco carteles del tutorial pasan a ser frases de Maela, Tobin y Rook (mismo sistema de cartel, otro modelo delante).
3. Nombre del pueblo en la pantalla de carga: "Vado Gris".
4. Gruk, Ferra y Ansel como NPC decorativos con una frase al interactuar. Sin funciones nuevas todavía.
5. Urzog con sus tres frases (entrar, perder, en el fuerte).
