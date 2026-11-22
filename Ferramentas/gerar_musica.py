"""
Gera a TRILHA SONORA ORIGINAL do jogo por sintese (sem samples de terceiros):
  - trilha_fases.wav  : loop espacial com arpejo, baixo, pad e chimbal (~38 s)
  - trilha_menu.wav   : pad calmo e misterioso para o menu (~32 s)
  - jingle_vitoria.wav: fanfarra curta de vitoria

Uso:  python Ferramentas/gerar_musica.py
Saida: Assets/Audio/Musica/*.wav
"""
import os
import wave

import numpy as np

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SAIDA = os.path.join(RAIZ, "Assets", "Audio", "Musica")
os.makedirs(SAIDA, exist_ok=True)
SR = 44100


def nota(n):
    """Numero MIDI -> frequencia em Hz."""
    return 440.0 * 2 ** ((n - 69) / 12)


def envelope(dur, ataque=0.01, soltura=0.2):
    t = np.arange(int(dur * SR)) / SR
    env = np.minimum(1, t / ataque)
    env *= np.clip((dur - t) / soltura, 0, 1)
    return env


def onda(freq, dur, tipo="sen"):
    t = np.arange(int(dur * SR)) / SR
    if tipo == "sen":
        return np.sin(2 * np.pi * freq * t)
    if tipo == "tri":
        return 2 * np.abs(2 * ((freq * t) % 1) - 1) - 1
    if tipo == "quad":
        return np.sign(np.sin(2 * np.pi * freq * t)) * 0.6
    raise ValueError(tipo)


def adicionar(buf, som, inicio):
    i = int(inicio * SR)
    fim = min(len(buf), i + len(som))
    buf[i:fim] += som[: fim - i]


def salvar(buf, nome):
    buf = buf / (np.max(np.abs(buf)) + 1e-9) * 0.85
    dados = (buf * 32767).astype(np.int16)
    caminho = os.path.join(SAIDA, nome)
    with wave.open(caminho, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(dados.tobytes())
    print("gerado:", caminho)


# Progressao em La menor (i - VI - III - VII): Am - F - C - G
ACORDES = [[57, 60, 64], [53, 57, 60], [48, 52, 55], [55, 59, 62]]

# ---------- TRILHA DAS FASES ----------
BPM = 104
batida = 60 / BPM
compassos = 16
dur_total = compassos * 4 * batida
buf = np.zeros(int(dur_total * SR) + SR)

for c in range(compassos):
    acorde = ACORDES[c % 4]
    inicio = c * 4 * batida
    # pad (acorde sustentado, suave)
    for n in acorde:
        s = onda(nota(n), 4 * batida, "sen") * envelope(4 * batida, 0.4, 0.6) * 0.10
        adicionar(buf, s, inicio)
    # baixo (fundamental uma oitava abaixo, nas batidas 1 e 3)
    for b in (0, 2):
        s = onda(nota(acorde[0] - 12), batida * 1.8, "tri") * envelope(batida * 1.8, 0.01, 0.3) * 0.32
        adicionar(buf, s, inicio + b * batida)
    # arpejo em colcheias (sobe e desce), so a partir do compasso 3
    if c >= 2:
        padrao = [0, 1, 2, 1 + 3, 2, 1, 0, 2]
        notas_arp = acorde + [acorde[0] + 12, acorde[1] + 12]
        for k, idx in enumerate(padrao):
            n = notas_arp[idx % len(notas_arp)] + 12
            s = onda(nota(n), batida / 2, "quad") * envelope(batida / 2, 0.005, 0.12) * 0.07
            adicionar(buf, s, inicio + k * batida / 2)
    # chimbal (ruido curto) a partir do compasso 5
    if c >= 4:
        for k in range(8):
            ruido = np.random.default_rng(c * 8 + k).uniform(-1, 1, int(0.04 * SR))
            ruido *= np.linspace(1, 0, len(ruido)) * (0.05 if k % 2 else 0.09)
            adicionar(buf, ruido, inicio + k * batida / 2)
    # bumbo nas batidas 1 e 3 a partir do compasso 5
    if c >= 4:
        for b in (0, 2):
            t = np.arange(int(0.18 * SR)) / SR
            bumbo = np.sin(2 * np.pi * (120 * np.exp(-t * 18) + 40) * t) * np.exp(-t * 14) * 0.5
            adicionar(buf, bumbo, inicio + b * batida)

salvar(buf[: int(dur_total * SR)], "trilha_fases.wav")

# ---------- TRILHA DO MENU ----------
BPM = 70
batida = 60 / BPM
compassos = 8
dur_total = compassos * 4 * batida
buf = np.zeros(int(dur_total * SR) + SR * 2)
for c in range(compassos):
    acorde = ACORDES[c % 4]
    inicio = c * 4 * batida
    for n in acorde + [acorde[0] - 12]:
        s = onda(nota(n), 4 * batida + 1, "sen") * envelope(4 * batida + 1, 1.0, 1.2) * 0.12
        adicionar(buf, s, inicio)
    # sininhos espaciais
    for k, idx in enumerate([2, 0, 1, 2]):
        n = acorde[idx] + 24
        s = onda(nota(n), 1.2, "sen") * envelope(1.2, 0.005, 1.1) * 0.06
        adicionar(buf, s, inicio + k * batida)
salvar(buf[: int(dur_total * SR)], "trilha_menu.wav")

# ---------- JINGLE DE VITORIA ----------
buf = np.zeros(int(3.0 * SR))
melodia = [(72, 0.0), (76, 0.15), (79, 0.3), (84, 0.45), (79, 0.75), (84, 0.9)]
for n, t0 in melodia:
    dur = 0.6 if t0 >= 0.9 else 0.25
    s = onda(nota(n), dur, "quad") * envelope(dur, 0.005, 0.15) * 0.25
    adicionar(buf, s, t0)
for n in (60, 64, 67, 72):
    s = onda(nota(n), 2.0, "sen") * envelope(2.0, 0.05, 1.5) * 0.12
    adicionar(buf, s, 0.9)
salvar(buf, "jingle_vitoria.wav")
