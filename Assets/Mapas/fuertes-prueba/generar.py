import json, sys
def anillo(x0,z0,x1,z1):
    return [(x,z) for x in range(x0,x1+1) for z in range(z0,z1+1) if x in (x0,x1) or z in (z0,z1)]
def guardar(nombre, piezas):
    m=sum(1 for p in piezas if p['tipo'] in ('muro_madera','muro_piedra','puerta'))
    t=sum(1 for p in piezas if p['tipo']=='trampa')
    d=len(piezas)-m-t
    assert m<=40 and t<=6 and d<=4, (nombre,m,t,d)
    xs=[(p['x'],p['z']) for p in piezas]
    assert (8,7) not in xs and (1,8) not in xs, nombre
    assert all(1<=x<=14 and 1<=z<=14 for x,z in xs), nombre
    assert len(set(xs))==len(xs), nombre
    json.dump({'version':1,'piezas':piezas}, open(nombre+'.json','w',encoding='utf-8'), indent=1)
    print(nombre, 'muros',m,'trampas',t,'defensores',d)
P=lambda x,z,t,v=0: {'x':x,'z':z,'tipo':t,'vida_extra':v}
# Fácil: una línea de madera que se puede rodear, un orco junto al cofre y una trampa.
guardar('facil', [P(5,z,'muro_madera') for z in range(4,12)] + [P(9,8,'orco_defensor'), P(6,8,'trampa')])
# Medio: anillo de piedra alrededor del cofre con 2 orcos dentro; 3 trampas delante del lado oeste.
medio=[P(x,z,'muro_piedra') for x,z in anillo(6,5,10,9)]
medio+=[P(7,8,'orco_defensor'),P(9,6,'orco_defensor'),P(5,6,'trampa'),P(5,7,'trampa'),P(5,8,'trampa')]
guardar('medio', medio)
# Difícil: 40 muros en tres capas (línea de madera fuera, anillo de piedra 7x7 y anillo de piedra 3x3);
# 2 orcos detrás de la madera, 2 esqueletos entre los anillos, trampas en el pasillo oeste entre anillos.
dif=[P(x,z,'muro_piedra') for x,z in anillo(7,6,9,8)] + [P(x,z,'muro_piedra') for x,z in anillo(5,4,11,10)]
dif+=[P(3,z,'muro_madera') for z in range(4,12)]
dif+=[P(6,z,'trampa') for z in range(5,10)] + [P(4,7,'trampa')]
dif+=[P(10,9,'esqueleto_defensor',1),P(10,5,'esqueleto_defensor',1),P(4,10,'orco_defensor',2),P(4,5,'orco_defensor',2)]
guardar('dificil', dif)
