#!/usr/bin/env python3
"""Icon packs v2: recolors/adaptations of Microsoft Fluent UI System Icons (MIT).

Base: Fluent 24 Regular paths (single fill #212121), canvas scaled to 64x64.
Mapping: sun/cloud/rain/snow/storm/fog <- Weather *; bell-none <- Alert;
bell-unread <- Alert + badge dot (composed); dnd-off <- Alert Off;
dnd-active <- Alert Snooze (snoozed bell = DND on); thunder = alias of storm.
Mono unread dot gets a pill-colored gap ring so it separates (adaptation).
Neon adds an SVG glow filter. Nothing is hand-drawn.
Run: python3 generate-packs.py  (needs cairosvg)
"""
import os
import re
import cairosvg

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.dirname(HERE)  # Packs/
BASE = os.path.join(HERE, 'fluent-base')

BASE_FILES = {
    'Weather/sun': 'sun.svg',
    'Weather/cloud': 'cloud.svg',
    'Weather/rain': 'rain.svg',
    'Weather/snow': 'snow.svg',
    'Weather/storm': 'storm.svg',
    'Weather/fog': 'fog.svg',
    'Statuses/dnd-active': 'bellsnooze.svg',
    'Statuses/dnd-off': 'belloff.svg',
    'Statuses/bell-unread': 'bell.svg',
    'Statuses/bell-none': 'bell.svg',
}
ALIAS = {'Weather/thunder': 'Weather/storm'}
DOT = '<circle cx="49.5" cy="14.5" r="9" fill="{dot}"/>{ring}'
RING = ('<circle cx="49.5" cy="14.5" r="9" fill="none" '
        'stroke="{ring}" stroke-width="2.5"/>')
GLOW_DEF = ('<defs><filter id="gl" x="-40%" y="-40%" width="180%" '
            'height="180%"><feGaussianBlur stdDeviation="2" result="b"/>'
            '<feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/>'
            '</feMerge></filter></defs>')


def inner(name):
    with open(os.path.join(BASE, BASE_FILES[name])) as f:
        s = f.read()
    m = re.search(r'<svg[^>]*>(.*)</svg>', s, re.S)
    return m.group(1)


PACKS = {
    # main: per-icon glyph color; dot: badge color; ring: gap-ring or '' ; glow
    'light': dict(c={'sun': '#1C1C1E', 'cloud': '#1C1C1E', 'rain': '#1C1C1E',
                     'snow': '#1C1C1E', 'storm': '#1C1C1E', 'fog': '#1C1C1E',
                     'dnd': '#1C1C1E', 'dndoff': '#1C1C1E',
                     'bellunread': '#1C1C1E', 'bellnone': '#1C1C1E'},
                  dot='#0A54FF', ring='#FFFFFF', glow=False),
    'dark': dict(c={'sun': '#F5F5F7', 'cloud': '#F5F5F7', 'rain': '#F5F5F7',
                    'snow': '#F5F5F7', 'storm': '#F5F5F7', 'fog': '#F5F5F7',
                    'dnd': '#F5F5F7', 'dndoff': '#F5F5F7',
                    'bellunread': '#F5F5F7', 'bellnone': '#F5F5F7'},
                 dot='#0A84FF', ring='', glow=False),
    'colorful': dict(c={'sun': '#FFB340', 'cloud': '#C7C7CC', 'rain': '#38BDF8',
                        'snow': '#BAE6FD', 'storm': '#FACC15', 'fog': '#94A3B8',
                        'dnd': '#FF453A', 'dndoff': '#8E8E93',
                        'bellunread': '#FFB340', 'bellnone': '#C7C7CC'},
                     dot='#0A84FF', ring='', glow=False),
    'mono': dict(c={'sun': '#F5F5F7', 'cloud': '#F5F5F7', 'rain': '#F5F5F7',
                    'snow': '#F5F5F7', 'storm': '#F5F5F7', 'fog': '#F5F5F7',
                    'dnd': '#F5F5F7', 'dndoff': '#F5F5F7',
                    'bellunread': '#F5F5F7', 'bellnone': '#F5F5F7'},
                 dot='#F5F5F7', ring='#0A0A0A', glow=False),
    'neon': dict(c={'sun': '#FFE600', 'cloud': '#00F0FF', 'rain': '#0A84FF',
                    'snow': '#C8FBFF', 'storm': '#FF2D95', 'fog': '#7D5CFF',
                    'dnd': '#FF375F', 'dndoff': '#B6FF00',
                    'bellunread': '#00F0FF', 'bellnone': '#00F0FF'},
                 dot='#FF2D95', ring='', glow=True),
    'pastel': dict(c={'sun': '#E8B84B', 'cloud': '#C9C9D6', 'rain': '#8AB8F5',
                      'snow': '#A9D6F5', 'storm': '#C4B5FD', 'fog': '#B8B8C8',
                      'dnd': '#F5A3AB', 'dndoff': '#A8A8B8',
                      'bellunread': '#B9A8EE', 'bellnone': '#C9C9D6'},
                  dot='#8AB8F5', ring='', glow=False),
}


def build(name, pal):
    body = inner(name).replace('#212121', pal['c'][({'sun':'sun','cloud':'cloud','rain':'rain','snow':'snow','storm':'storm','fog':'fog','dnd-active':'dnd','dnd-off':'dndoff','bell-unread':'bellunread','bell-none':'bellnone'}[name.split('/')[1]])] )
    glyph = (f'<g transform="translate(4,4) scale(2.3333)">{body}</g>')
    if name == 'Statuses/bell-unread':
        ring = RING.format(ring=pal['ring']) if pal['ring'] else ''
        glyph += DOT.format(dot=pal['dot'], ring=ring)
    if pal['glow']:
        return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">'
                f'{GLOW_DEF}<g filter="url(#gl)">{glyph}</g></svg>')
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">'
            f'{glyph}</svg>')


def main():
    total = 0
    for pack, pal in PACKS.items():
        made = {}
        for name in BASE_FILES:
            made[name] = build(name, pal).encode()
        for alias, target in ALIAS.items():
            made[alias] = made[target]
        for name, data in made.items():
            d = os.path.join(OUT, pack, os.path.dirname(name))
            os.makedirs(d, exist_ok=True)
            open(os.path.join(OUT, pack, name + '.svg'), 'wb').write(data)
            cairosvg.svg2png(bytestring=data,
                             write_to=os.path.join(OUT, pack, name + '.png'),
                             output_width=64, output_height=64)
            total += 2
    print(f'OK: {len(PACKS)} packs, {total} files -> {OUT}')


if __name__ == '__main__':
    main()
