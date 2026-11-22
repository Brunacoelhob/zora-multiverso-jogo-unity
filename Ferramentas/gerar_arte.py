"""
Gera a arte ORIGINAL do jogo que nao vem no pacote Kenney:
  - fundos espaciais de cada planeta (Lua, Marte, Terra, Jupiter) e do menu
  - sprite do portal do multiverso

Uso:  python Ferramentas/gerar_arte.py
Saida: Assets/Art/Gerados/*.png
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SAIDA = os.path.join(RAIZ, "Assets", "Art", "Gerados")
os.makedirs(SAIDA, exist_ok=True)

LARG, ALT = 1920, 1080


def gradiente(cor_topo, cor_base):
    """Ceu em degrade vertical."""
    img = Image.new("RGB", (LARG, ALT))
    px = img.load()
    for y in range(ALT):
        t = y / (ALT - 1)
        cor = tuple(int(cor_topo[i] + (cor_base[i] - cor_topo[i]) * t) for i in range(3))
        for x in range(LARG):
            px[x, y] = cor
    return img


def estrelas(img, quantidade, semente, alcance_y=1.0):
    """Pontinhos brilhantes (algumas estrelas maiores com brilho)."""
    rnd = random.Random(semente)
    d = ImageDraw.Draw(img, "RGBA")
    for _ in range(quantidade):
        x = rnd.randrange(LARG)
        y = rnd.randrange(int(ALT * alcance_y))
        r = rnd.choice([1, 1, 1, 2, 2, 3])
        a = rnd.randint(120, 255)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, a))
        if r == 3:  # estrela grande ganha uma cruz de brilho
            d.line([x - 8, y, x + 8, y], fill=(255, 255, 255, 90), width=1)
            d.line([x, y - 8, x, y + 8], fill=(255, 255, 255, 90), width=1)


def planeta(img, cx, cy, raio, cor_clara, cor_escura, faixas=None, aneis=False, brilho=None):
    """Desenha um planeta com sombreado (luz vindo da esquerda/cima)."""
    camada = Image.new("RGBA", img.size, (0, 0, 0, 0))
    px = camada.load()
    for y in range(max(0, cy - raio), min(ALT, cy + raio)):
        for x in range(max(0, cx - raio), min(LARG, cx + raio)):
            dx, dy = (x - cx) / raio, (y - cy) / raio
            d2 = dx * dx + dy * dy
            if d2 > 1:
                continue
            luz = max(0.0, min(1.0, 0.65 - 0.55 * dx - 0.35 * dy))
            cor = [cor_escura[i] + (cor_clara[i] - cor_escura[i]) * luz for i in range(3)]
            if faixas:
                for (pos, largura, tom) in faixas:
                    if abs(dy - pos) < largura:
                        cor = [c * tom for c in cor]
            borda = 1.0 - max(0.0, (math.sqrt(d2) - 0.97) / 0.03)
            px[x, y] = tuple(int(max(0, min(255, c))) for c in cor) + (int(255 * borda),)
    if brilho:
        halo = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(halo).ellipse(
            [cx - raio - 30, cy - raio - 30, cx + raio + 30, cy + raio + 30], fill=brilho + (90,))
        halo = halo.filter(ImageFilter.GaussianBlur(28))
        img.paste(halo, (0, 0), halo)
    if aneis:
        d = ImageDraw.Draw(img, "RGBA")
        d.ellipse([cx - raio * 2, cy - raio * 0.35, cx + raio * 2, cy + raio * 0.35],
                  outline=(230, 210, 170, 170), width=10)
    img.paste(camada, (0, 0), camada)


def crateras(img, cx, cy, raio, qtd, semente):
    rnd = random.Random(semente)
    d = ImageDraw.Draw(img, "RGBA")
    for _ in range(qtd):
        ang, dist = rnd.uniform(0, 6.28), rnd.uniform(0, raio * 0.8)
        x, y = cx + math.cos(ang) * dist, cy + math.sin(ang) * dist
        r = rnd.randint(int(raio * 0.05), int(raio * 0.14))
        d.ellipse([x - r, y - r, x + r, y + r], fill=(0, 0, 0, 45))


def horizonte(img, cor, altura_base, amplitude, semente, opac=255):
    """Silhueta de montanhas/dunas no rodape (da profundidade a cena)."""
    rnd = random.Random(semente)
    d = ImageDraw.Draw(img, "RGBA")
    pontos = [(0, ALT)]
    fases = [rnd.uniform(0, 6.28) for _ in range(3)]
    for x in range(0, LARG + 20, 20):
        y = altura_base
        y += math.sin(x / 260 + fases[0]) * amplitude
        y += math.sin(x / 110 + fases[1]) * amplitude * 0.4
        y += math.sin(x / 45 + fases[2]) * amplitude * 0.12
        pontos.append((x, int(y)))
    pontos.append((LARG, ALT))
    d.polygon(pontos, fill=cor + (opac,))


def salvar(img, nome):
    caminho = os.path.join(SAIDA, nome)
    img.save(caminho)
    print("gerado:", caminho)


# ---------- LUA: ceu negro, Terra azul no ceu, solo cinza ----------
img = gradiente((5, 6, 18), (28, 30, 48))
estrelas(img, 700, 1)
planeta(img, 1450, 260, 150, (110, 170, 255), (20, 50, 120), brilho=(80, 140, 255))
d = ImageDraw.Draw(img, "RGBA")
for (x, y, r) in [(1400, 220, 50), (1500, 300, 40), (1430, 330, 30)]:  # continentes
    d.ellipse([x - r, y - r * 0.6, x + r, y + r * 0.6], fill=(70, 160, 90, 150))
horizonte(img, (70, 72, 85), 830, 50, 11)
horizonte(img, (95, 97, 110), 920, 35, 12)
salvar(img, "fundo_lua.png")

# ---------- MARTE: ceu poeirento laranja, duas luas ----------
img = gradiente((70, 25, 20), (205, 120, 75))
estrelas(img, 180, 2, 0.45)
planeta(img, 400, 230, 55, (200, 180, 160), (90, 70, 60))   # Fobos
planeta(img, 1600, 160, 30, (190, 170, 150), (80, 60, 50))  # Deimos
horizonte(img, (150, 60, 35), 800, 70, 21)
horizonte(img, (115, 45, 30), 900, 45, 22)
salvar(img, "fundo_marte.png")

# ---------- TERRA: ceu azul, Lua palida de dia ----------
img = gradiente((70, 140, 230), (200, 230, 255))
planeta(img, 1550, 220, 70, (245, 245, 250), (170, 170, 185))
crateras(img, 1550, 220, 70, 7, 3)
d = ImageDraw.Draw(img, "RGBA")
rnd = random.Random(5)
for _ in range(9):  # nuvens
    x, y = rnd.randrange(LARG), rnd.randrange(80, 600)
    for k in range(5):
        r = rnd.randint(40, 80)
        d.ellipse([x + k * 45 - r, y - r * 0.6, x + k * 45 + r, y + r * 0.6], fill=(255, 255, 255, 160))
horizonte(img, (120, 185, 120), 820, 60, 31)
horizonte(img, (80, 150, 90), 920, 40, 32)
salvar(img, "fundo_terra.png")

# ---------- JUPITER: ceu escuro e o gigante gasoso enorme ----------
img = gradiente((25, 12, 30), (70, 35, 45))
estrelas(img, 350, 4)
faixas = [(-0.55, 0.06, 0.8), (-0.3, 0.09, 1.15), (-0.05, 0.07, 0.75),
          (0.2, 0.1, 1.1), (0.45, 0.06, 0.8), (0.7, 0.08, 1.12)]
planeta(img, 1250, 380, 330, (235, 190, 140), (120, 70, 45), faixas=faixas, brilho=(255, 170, 90))
d = ImageDraw.Draw(img, "RGBA")
d.ellipse([1300, 520, 1420, 580], fill=(190, 80, 50, 200))  # grande mancha vermelha
horizonte(img, (75, 35, 70), 860, 45, 41)
horizonte(img, (55, 25, 55), 940, 30, 42)
salvar(img, "fundo_jupiter.png")

# ---------- MENU: multiverso (varios planetas + rasgo roxo) ----------
img = gradiente((10, 5, 30), (45, 20, 70))
estrelas(img, 900, 6)
rasgo = Image.new("RGBA", img.size, (0, 0, 0, 0))
dr = ImageDraw.Draw(rasgo)
pts = [(300 + i * 90, 540 + int(math.sin(i * 1.3) * 160)) for i in range(16)]
dr.line(pts, fill=(200, 100, 255, 255), width=26)
rasgo = rasgo.filter(ImageFilter.GaussianBlur(14))
img.paste(rasgo, (0, 0), rasgo)
ImageDraw.Draw(img, "RGBA").line(pts, fill=(255, 230, 255, 230), width=5)
planeta(img, 260, 220, 90, (220, 220, 230), (90, 90, 110))
planeta(img, 1650, 260, 120, (235, 190, 140), (120, 70, 45), faixas=faixas[:3], aneis=True)
planeta(img, 1500, 850, 70, (230, 120, 80), (110, 40, 30))
planeta(img, 420, 880, 60, (110, 170, 255), (20, 50, 120))
salvar(img, "fundo_menu.png")

# ---------- PORTAL: anel brilhante 128x128 ----------
TAM = 128
portal = Image.new("RGBA", (TAM, TAM), (0, 0, 0, 0))
px = portal.load()
for y in range(TAM):
    for x in range(TAM):
        dx, dy = (x - 63.5) / 64, (y - 63.5) / 64
        r = math.sqrt(dx * dx + dy * dy)
        ang = math.atan2(dy, dx)
        if r > 1:
            continue
        espiral = 0.5 + 0.5 * math.sin(ang * 3 + r * 14)
        anel = max(0.0, 1 - abs(r - 0.78) / 0.2)
        miolo = max(0.0, 1 - r / 0.7) * 0.85
        a = min(1.0, anel + miolo * (0.6 + 0.4 * espiral))
        cor = (int(150 + 105 * anel), int(60 + 120 * espiral * miolo + 40 * anel), 255)
        px[x, y] = cor + (int(255 * a),)
salvar(portal, "portal.png")

# ---------- PARTICULA: bolinha suave para efeitos ----------
part = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
pp = part.load()
for y in range(32):
    for x in range(32):
        r = math.sqrt((x - 15.5) ** 2 + (y - 15.5) ** 2) / 16
        if r < 1:
            pp[x, y] = (255, 255, 255, int(255 * (1 - r) ** 1.5))
salvar(part, "particula.png")
