"""
Gera os mapas das fases (Assets/Fases/*.txt) a partir de uma descricao por blocos.
Os .txt sao lidos pelo construtor de cenas da Unity (Assets/Scripts/Editor/ConstrutorJogo.cs).

LEGENDA DOS MAPAS
  #  terreno solido            =  plataforma (atravessa por baixo)
  H  escada                    ^  espinhos
  o  moeda                     *  fragmento de estrela (precisa de todos para abrir o portal)
  +  coracao (+1 vida)         F  bandeira de checkpoint
  S  slime (inimigo andante)   C  caracol (inimigo andante lento)
  B  abelha (inimigo voador)   P  inicio da Zora
  X  portal do multiverso (fim da fase)

FISICA USADA NO DESENHO (pulo com forca 13; gravidade = 30 * (g/9,81)^(1/3)):
  planeta   altura do pulo   subida segura   buraco seguro (andando / correndo)
  Lua         ~5,1 blocos       4 blocos          6 / 9
  Marte       ~3,9 blocos       3 blocos          5 / 7
  Terra       ~2,8 blocos       2 blocos          3 / 5
  Jupiter     ~2,1 blocos       1 bloco           2 / 4
"""
import os

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SAIDA = os.path.join(RAIZ, "Assets", "Fases")
os.makedirs(SAIDA, exist_ok=True)

ALTURA = 16


class Mapa:
    def __init__(self, largura):
        self.l = largura
        self.g = [[" "] * largura for _ in range(ALTURA)]
        # paredes nas pontas: a Zora nao sai do mapa
        self.chao(0, 0, 12)
        self.chao(largura - 1, largura - 1, 12)

    def por(self, x, y, c):
        self.g[ALTURA - 1 - y][x] = c

    def chao(self, x0, x1, altura):
        """Terreno do x0 ao x1 (inclusive) com 'altura' blocos. A Zora pisa em y = altura."""
        for x in range(x0, x1 + 1):
            for y in range(altura):
                self.por(x, y, "#")

    def plataforma(self, x0, x1, y):
        for x in range(x0, x1 + 1):
            self.por(x, y, "=")

    def escada(self, x, y0, y1):
        for y in range(y0, y1 + 1):
            self.por(x, y, "H")

    def varios(self, c, xs, y):
        for x in xs:
            self.por(x, y, c)

    def texto(self):
        return "\n".join("".join(linha).rstrip() for linha in self.g) + "\n"

    def validar(self, nome):
        t = self.texto()
        assert t.count("P") == 1, nome + ": precisa de 1 P"
        assert t.count("X") == 1, nome + ": precisa de 1 X"
        assert t.count("*") == 3, nome + ": precisa de 3 fragmentos"


def salvar(m, nome):
    m.validar(nome)
    caminho = os.path.join(SAIDA, nome + ".txt")
    with open(caminho, "w", encoding="utf-8", newline="\n") as f:
        f.write(m.texto())
    print("gerado:", caminho)
    print(m.texto())


# ============ FASE 1 - LUA (aprender: andar, pular, buracos, moedas) ============
m = Mapa(92)
m.chao(0, 14, 3)
m.por(2, 3, "P")
m.varios("o", range(6, 10), 4)
m.varios("o", [16, 18], 6); m.por(17, 7, "o")         # arco de moedas sobre o 1o buraco
m.chao(20, 44, 3)
m.varios("^", [25, 26], 3)                             # primeiros espinhos
m.plataforma(23, 28, 6)
m.por(26, 7, "*")                                      # fragmento 1 (na plataforma)
m.por(38, 3, "S")
m.escada(44, 3, 5)                                     # escada opcional (apresenta a mecanica)
m.chao(45, 55, 6)
m.varios("o", range(47, 52), 6)
m.por(53, 6, "+")
m.varios("o", [58, 60], 8); m.por(59, 9, "o")          # arco sobre o buraco grande
m.chao(62, 66, 3)
m.por(64, 3, "F")                                      # checkpoint
m.plataforma(69, 70, 4)                                # plataforma no meio do buraco
m.por(69, 7, "*")                                      # fragmento 2 (pular da plataforma)
m.chao(73, 90, 3)
m.plataforma(76, 79, 6)
m.por(77, 7, "*")                                      # fragmento 3
m.varios("o", [80, 81, 82], 4)
m.por(84, 3, "S")
m.por(88, 3, "X")
salvar(m, "Fase1_Lua")

# ============ FASE 2 - MARTE (aprender: escadas, plataformas em escada) ============
m = Mapa(100)
m.chao(0, 12, 3)
m.por(2, 3, "P")
m.varios("o", range(5, 9), 4)
m.chao(13, 22, 3)
m.varios("^", [15, 16, 17], 3)
m.escada(22, 3, 7)                                     # paredao de 5 blocos: so com escada
m.chao(23, 33, 8)
m.varios("o", range(25, 31), 8)
m.por(32, 8, "*")                                      # fragmento 1 (em cima do paredao)
m.chao(34, 48, 3)                                      # chao logo apos o paredao (descida segura)
m.por(42, 3, "S")
m.plataforma(50, 53, 5); m.varios("o", [51, 52], 6)    # escadinha de plataformas
m.plataforma(56, 59, 8); m.varios("o", [57, 58], 9)
m.plataforma(62, 65, 11)
m.por(64, 12, "*")                                     # fragmento 2 (no alto)
m.chao(67, 82, 4)
m.por(68, 4, "F")
m.por(70, 5, "+")
m.por(72, 4, "S")
m.por(75, 4, "^")
m.por(79, 4, "S")
m.escada(82, 4, 8)
m.chao(83, 90, 9)
m.varios("o", [84, 85, 86], 9)
m.por(88, 9, "*")                                      # fragmento 3
m.chao(91, 98, 3)
m.por(96, 3, "X")
salvar(m, "Fase2_Marte")

# ============ FASE 3 - TERRA (precisao: abelhas, mais inimigos, pulos curtos) ============
m = Mapa(110)
m.chao(0, 10, 3)
m.por(2, 3, "P")
m.varios("o", range(5, 9), 4)
m.chao(14, 24, 3)
m.por(19, 6, "B")
m.por(22, 3, "^")
m.chao(25, 28, 5)
m.chao(29, 32, 7)
m.plataforma(33, 35, 7)
m.por(34, 8, "*")                                      # fragmento 1
m.chao(37, 50, 5)
m.por(40, 5, "S"); m.por(47, 5, "S")
m.varios("o", [43, 44, 45], 6)
m.plataforma(52, 53, 5)
m.chao(55, 70, 5)
m.por(56, 5, "F")
m.varios("^", [59, 60], 5)
m.por(62, 8, "B")
m.varios("o", range(64, 68), 5)
m.por(68, 5, "+")
m.escada(70, 5, 9)
m.chao(71, 80, 10)
m.varios("o", range(73, 77), 10)
m.por(75, 13, "B")
m.por(78, 10, "*")                                     # fragmento 2 (topo do paredao)
m.chao(81, 108, 3)
m.por(85, 3, "S")
m.plataforma(86, 88, 4)
m.plataforma(90, 92, 6)
m.por(91, 7, "*")                                      # fragmento 3
m.por(94, 3, "S")
m.varios("o", [98, 99, 100], 4)
m.por(106, 3, "X")
salvar(m, "Fase3_Terra")

# ============ FASE 4 - JUPITER (gravidade forte: escalar e correr sao essenciais) ============
m = Mapa(100)
m.chao(0, 12, 3)
m.por(2, 3, "P")
m.varios("o", range(5, 9), 4)
m.chao(13, 18, 4)
m.chao(19, 24, 5)
m.escada(24, 5, 7)
m.chao(25, 34, 8)
m.varios("o", range(26, 29), 8)
m.por(31, 8, "C")
m.chao(35, 46, 4)
m.por(40, 4, "S")
m.por(45, 4, "+")
m.chao(49, 58, 4)                                      # buraco de 2 (47-48)
m.por(50, 4, "F")
m.por(53, 4, "^")
m.por(56, 4, "S")
m.escada(58, 4, 8)
m.chao(59, 66, 9)
m.varios("o", [60, 61, 62], 9)
m.por(64, 9, "*")                                      # fragmento 1
m.chao(67, 80, 3)
m.por(70, 3, "S")
m.por(73, 5, "B")
m.por(74, 3, "*")                                      # fragmento 2 (protegido pela abelha)
m.por(78, 3, "C")
m.chao(83, 98, 3)                                      # buraco de 2 (81-82)
m.escada(88, 3, 8)                                     # escada no meio do campo
m.plataforma(89, 91, 8)
m.por(90, 9, "*")                                      # fragmento 3 (no alto da escada)
m.varios("o", [85, 86], 4)
m.por(96, 3, "X")
salvar(m, "Fase4_Jupiter")
