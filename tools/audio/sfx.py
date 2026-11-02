"""Sound effect recipes for Saucer Siege. Each function returns a rendered Track."""

from synth import Track, note_freq, saw, square, triangle


def pulse(duty):
    return lambda p: square(p, duty)


def click():
    """A menu tick."""
    t = Track(0.06)
    t.add_tone(0, 0.025, 1300, pulse(0.5), 0.18, release=0.015)
    return t


def denied():
    """Two low buzzes: not allowed or not enough money."""
    t = Track(0.3)
    t.add_tone(0, 0.08, 160, pulse(0.5), 0.2, release=0.02)
    t.add_tone(0.12, 0.08, 130, pulse(0.5), 0.2, release=0.02)
    return t


def build():
    """Stone blocks thud into place."""
    t = Track(0.45)
    t.add_tone(0, 0.08, 180, triangle, 0.55, freq_end=90, release=0.04)
    t.add_noise(0, 0.08, 0.3, smoothing=0.7, decay_power=2, seed=11)
    t.add_tone(0.12, 0.08, 220, triangle, 0.5, freq_end=110, release=0.04)
    t.add_noise(0.12, 0.08, 0.25, smoothing=0.7, decay_power=2, seed=12)
    t.add_tone(0.22, 0.12, note_freq("G5"), pulse(0.25), 0.12, release=0.06)
    return t


def upgrade():
    """A rising sparkle."""
    t = Track(0.6)
    for i, n in enumerate(["C5", "E5", "G5", "C6", "E6"]):
        t.add_tone(i * 0.07, 0.1, note_freq(n), pulse(0.25), 0.15, release=0.08)
    return t


def sell():
    """Coins tumble into the purse."""
    t = Track(0.5)
    for i, n in enumerate(["B5", "E6", "B5", "G#6"]):
        t.add_tone(i * 0.06, 0.06, note_freq(n), pulse(0.5), 0.12, release=0.05)
    return t


def ballista():
    """A taut string snaps and a bolt whistles off."""
    t = Track(0.2)
    t.add_tone(0, 0.05, 420, pulse(0.125), 0.16, freq_end=180, release=0.02)
    t.add_noise(0, 0.08, 0.16, smoothing=0.2, decay_power=2.5, seed=21)
    return t


def cannon():
    """A short cannon boom."""
    t = Track(0.4)
    t.add_noise(0, 0.22, 0.6, smoothing=0.85, decay_power=1.8, seed=22)
    t.add_tone(0, 0.15, 120, triangle, 0.6, freq_end=45, release=0.05)
    return t


def frost():
    """A glassy chirp of cold."""
    t = Track(0.3)
    t.add_tone(0, 0.12, 1800, triangle, 0.18, freq_end=2600, release=0.08, vibrato=0.02)
    t.add_tone(0.03, 0.1, 2400, triangle, 0.1, freq_end=1700, release=0.08)
    return t


def mortar():
    """The catapult arm creaks and throws."""
    t = Track(0.45)
    t.add_tone(0, 0.18, 90, saw, 0.2, freq_end=150, release=0.03)
    t.add_noise(0.15, 0.16, 0.35, smoothing=0.6, decay_power=1.5, seed=23)
    t.add_tone(0.16, 0.12, 300, triangle, 0.25, freq_end=140, release=0.05)
    return t


def explosion():
    """A blast of rock and fire."""
    t = Track(0.8)
    t.add_noise(0, 0.6, 0.75, smoothing=0.9, decay_power=1.3, seed=24)
    t.add_tone(0, 0.35, 95, triangle, 0.7, freq_end=30, release=0.1)
    return t


def hit():
    """A bolt pings off a hull."""
    t = Track(0.1)
    t.add_tone(0, 0.04, 900, pulse(0.25), 0.14, freq_end=600, release=0.02)
    return t


def saucer_down():
    """A saucer sputters and pops."""
    t = Track(0.5)
    t.add_tone(0, 0.25, 900, pulse(0.25), 0.16, freq_end=120, release=0.05, vibrato=0.05)
    t.add_noise(0.12, 0.2, 0.4, smoothing=0.75, decay_power=1.6, seed=25)
    return t


def mothership_down():
    """The mothership breaks apart in a long rumble."""
    t = Track(2.0)
    t.add_tone(0, 1.2, 600, saw, 0.18, freq_end=60, release=0.3, vibrato=0.06)
    for i in range(5):
        t.add_noise(i * 0.22, 0.5, 0.55, smoothing=0.88, decay_power=1.4, seed=30 + i)
        t.add_tone(i * 0.22, 0.25, 110 - i * 10, triangle, 0.45, freq_end=35, release=0.08)
    return t


def leak():
    """An alarm: a saucer reached the castle."""
    t = Track(0.6)
    t.add_tone(0, 0.18, 660, pulse(0.5), 0.18, freq_end=440, release=0.03)
    t.add_tone(0.22, 0.18, 660, pulse(0.5), 0.18, freq_end=440, release=0.03)
    t.add_noise(0, 0.15, 0.25, smoothing=0.7, decay_power=2, seed=26)
    return t


def wave_start():
    """A horn call."""
    t = Track(0.9)
    for i, (n, d) in enumerate([("G4", 0.15), ("C5", 0.15), ("E5", 0.45)]):
        t.add_tone(i * 0.17, d, note_freq(n), saw, 0.12, attack=0.02, release=0.1, vibrato=0.01)
        t.add_tone(i * 0.17, d, note_freq(n) / 2, triangle, 0.25, attack=0.02, release=0.1)
    return t


def wave_clear():
    """A short happy jingle."""
    t = Track(0.8)
    for i, n in enumerate(["E5", "G5", "C6"]):
        t.add_tone(i * 0.1, 0.12, note_freq(n), pulse(0.25), 0.16, release=0.1)
    t.add_tone(0.3, 0.3, note_freq("G6"), triangle, 0.15, release=0.15)
    return t


def victory():
    """A fanfare."""
    t = Track(2.0)
    notes = [("C5", 0.12), ("E5", 0.12), ("G5", 0.12), ("C6", 0.36), ("G5", 0.12), ("C6", 0.7)]
    at = 0.0
    for n, d in notes:
        t.add_tone(at, d, note_freq(n), pulse(0.25), 0.18, release=0.08, vibrato=0.006)
        t.add_tone(at, d, note_freq(n) / 2, triangle, 0.3, release=0.08)
        at += d + 0.03
    return t


def defeat():
    """A sad descending phrase."""
    t = Track(2.0)
    at = 0.0
    for n, d in [("G4", 0.3), ("F#4", 0.3), ("F4", 0.3), ("E4", 0.8)]:
        t.add_tone(at, d, note_freq(n), triangle, 0.35, release=0.12, vibrato=0.01)
        t.add_tone(at, d, note_freq(n) / 2, pulse(0.5), 0.08, release=0.12)
        at += d + 0.04
    return t


EFFECTS = {
    "click.ogg": click,
    "denied.ogg": denied,
    "build.ogg": build,
    "upgrade.ogg": upgrade,
    "sell.ogg": sell,
    "ballista.ogg": ballista,
    "cannon.ogg": cannon,
    "frost.ogg": frost,
    "mortar.ogg": mortar,
    "explosion.ogg": explosion,
    "hit.ogg": hit,
    "saucer_down.ogg": saucer_down,
    "mothership_down.ogg": mothership_down,
    "leak.ogg": leak,
    "wave_start.ogg": wave_start,
    "wave_clear.ogg": wave_clear,
    "victory.ogg": victory,
    "defeat.ogg": defeat,
}
