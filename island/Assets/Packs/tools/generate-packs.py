#!/usr/bin/env python3
"""Generate WinUI island icon packs: SVG + PNG 64x64.

Packs: light, dark, colorful, mono, neon, pastel.
Layout per pack: Weather/{sun,cloud,rain,snow,storm,fog,thunder}.svg/.png
                  Statuses/{dnd-active,dnd-off,bell-unread,bell-none}.svg/.png
`thunder` is an alias of `storm` (PR #6 convention: code uses `storm`).
Run: python3 generate-packs.py  (needs cairosvg)
"""
import os
import cairosvg

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.dirname(HERE)  # Packs/

CLOUD = (
    '<circle cx="24" cy="36" r="10" fill="{c1}"/>'
    '<circle cx="34" cy="30" r="13" fill="{c1}"/>'
    '<circle cx="43" cy="37" r="9" fill="{c1}"/>'
    '<rect x="15" y="36" width="36" height="11" rx="5.5" fill="{c1}"/>'
)
CLOUD_SM = (
    '<circle cx="24" cy="28" r="8" fill="{c1}"/>'
    '<circle cx="33" cy="23" r="10.5" fill="{c1}"/>'
    '<circle cx="41" cy="29" r="7" fill="{c1}"/>'
    '<rect x="17" y="28" width="32" height="9" rx="4.5" fill="{c1}"/>'
)
RAYS = ''.join(
    f'<line x1="32" y1="8" x2="32" y2="15" stroke="{{sun}}" stroke-width="4"'
    f' stroke-linecap="round" transform="rotate({a} 32 30)"/>'
    for a in (0, 45, 90, 135, 180, 225, 270, 315)
)
BELL = (
    '<ellipse cx="32" cy="13" rx="5" ry="5" fill="{c1}"/>'
    '<path d="M27 17 L20 39 L44 39 L37 17 Z" fill="{c1}"/>'
    '<ellipse cx="32" cy="45" rx="4.5" ry="4.5" fill="{c1}"/>'
)


def svg(body, glow=False):
    f = ('<defs><filter id="gl" x="-40%" y="-40%" width="180%" height="180%">'
         '<feGaussianBlur stdDeviation="2.2" result="b"/>'
         '<feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/>'
         '</feMerge></filter></defs>')
    g1, g2 = ('<g filter="url(#gl)">', '</g>') if glow else ('<g>', '</g>')
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">'
            f'{f if glow else ""}{g1}{body}{g2}</svg>')


def icons(p):
    sun = f'<circle cx="32" cy="30" r="11" fill="{p["sun"]}"/>' + RAYS.format(sun=p["sun"])
    return {
        'Weather/sun': sun,
        'Weather/cloud': CLOUD.format(c1=p["cloud"]),
        'Weather/rain': CLOUD_SM.format(c1=p["cloud"]) + ''.join(
            f'<line x1="{x}" y1="43" x2="{x - 3}" y2="52" stroke="{p["drop"]}"'
            ' stroke-width="4" stroke-linecap="round"/>' for x in (25, 34, 43)),
        'Weather/snow': CLOUD_SM.format(c1=p["cloud"]) + ''.join(
            f'<g stroke="{p["flake"]}" stroke-width="2.6" stroke-linecap="round">'
            f'<line x1="{x - 4}" y1="46" x2="{x + 4}" y2="46"/>'
            f'<line x1="{x}" y1="42" x2="{x}" y2="50"/></g>' for x in (24, 33, 42)),
        'Weather/storm': CLOUD_SM.format(c1=p["cloud"]) +
            f'<polygon points="35,36 26,49 31.5,49 29,59 39,47 33.5,47" fill="{p["bolt"]}"'
            ' stroke-linejoin="round"/>',
        'Weather/fog': CLOUD_SM.format(c1=p["cloud"]) + ''.join(
            f'<line x1="{x}" y1="{y}" x2="{x + 14}" y2="{y}" stroke="{p["fogline"]}"'
            ' stroke-width="3.4" stroke-linecap="round"/>'
            for x, y in ((18, 44), (28, 49), (20, 54))),
        'Statuses/dnd-active': f'<circle cx="32" cy="32" r="14" fill="{p["dnd"]}"/>' +
            f'<rect x="22" y="29" width="20" height="6" rx="3" fill="{p["dndbar"]}"/>',
        'Statuses/dnd-off': f'<circle cx="32" cy="32" r="14" fill="none" stroke="{p["c2"]}"'
            ' stroke-width="3.5"/>' +
            f'<line x1="23" y1="32" x2="41" y2="32" stroke="{p["c2"]}"'
            ' stroke-width="3.5" stroke-linecap="round"/>',
        'Statuses/bell-unread': BELL.format(c1=p["bell"]) +
            f'<circle cx="46" cy="15" r="8.5" fill="{p["dot"]}"/>' +
            '<circle cx="46" cy="15" r="8.5" fill="none" stroke="#FFFFFF"'
            ' stroke-width="1.6" opacity="0.85"/>',
        'Statuses/bell-none': BELL.format(c1=p["bell"]),
    }


PACKS = {
    # primary / secondary / accent set per pack
    'light': dict(sun='#B26A00', cloud='#1C1C1E', drop='#0A54FF', flake='#F5F5F7',
                  bolt='#B26A00', fogline='#48484A', bell='#1C1C1E', dot='#0A54FF',
                  dnd='#E0301E', dndbar='#FFFFFF', c2='#48484A', glow=False),
    'dark': dict(sun='#FFC83C', cloud='#F5F5F7', drop='#64A6FF', flake='#D7D7DE',
                 bolt='#FFC83C', fogline='#AEAEB2', bell='#F5F5F7', dot='#0A84FF',
                 dnd='#FF453A', dndbar='#FFFFFF', c2='#AEAEB2', glow=False),
    'colorful': dict(sun='#FFC83C', cloud='#8E8E93', drop='#0A84FF', flake='#64D2FF',
                     bolt='#FFD60A', fogline='#AEAEB2', bell='#FF9F0A', dot='#0A84FF',
                     dnd='#FF3B30', dndbar='#FFFFFF', c2='#636366', glow=False),
    'mono': dict(sun='#F5F5F7', cloud='#F5F5F7', drop='#F5F5F7', flake='#F5F5F7',
                 bolt='#0A0A0A', fogline='#F5F5F7', bell='#F5F5F7', dot='#F5F5F7',
                 dnd='#F5F5F7', dndbar='#0A0A0A', c2='#F5F5F7', glow=False),
    'neon': dict(sun='#FFE600', cloud='#00F0FF', drop='#0A84FF', flake='#C8FBFF',
                 bolt='#FF2D95', fogline='#7D5CFF', bell='#00F0FF', dot='#FF2D95',
                 dnd='#FF375F', dndbar='#FFFFFF', c2='#B6FF00', glow=True),
    'pastel': dict(sun='#FFB84D', cloud='#D9D9E3', drop='#8AB8F5', flake='#B8E0F5',
                   bolt='#FFD97A', fogline='#C4C4D4', bell='#B9A8EE', dot='#8AB8F5',
                   dnd='#FF9AA2', dndbar='#FFFFFF', c2='#A8A8B8', glow=False),
}

# mono bolt must read on white cloud -> dark handled above; mono uses dark bolt on light cloud? cloud is white -> bolt dark ok.
ALIAS = {'Weather/thunder': 'Weather/storm'}


def main():
    total = 0
    for pack, pal in PACKS.items():
        glow = pal.pop('glow')
        ic = icons(pal)
        for name, target in ALIAS.items():
            ic[name] = ic[target]
        for name, body in ic.items():
            d = os.path.join(OUT, pack, os.path.dirname(name))
            os.makedirs(d, exist_ok=True)
            sp = os.path.join(OUT, pack, name + '.svg')
            data = svg(body, glow).encode()
            open(sp, 'wb').write(data)
            pp = os.path.join(OUT, pack, name + '.png')
            cairosvg.svg2png(bytestring=data, write_to=pp,
                             output_width=64, output_height=64)
            total += 2
    print(f'OK: {len(PACKS)} packs, {total} files -> {OUT}')


if __name__ == '__main__':
    main()
