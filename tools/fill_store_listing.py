# Fills the listings file that Partner Center exports ("Export listings") from store/listing.md,
# and builds a folder for "Import listings > Import folder": the file, the screenshots from
# dist/store-screenshots and the logos from store/images.
#   python tools/fill_store_listing.py <exported .csv> <folder to create the import folder in>
# Partner Center refuses the whole import if any field is wrong, and lists why under
# "View errors for last import".
import csv, io, os, re, shutil, sys

repo = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
exported, parent = sys.argv[1], sys.argv[2]
# Image paths in the file start with the name of the folder that is imported, with forward slashes.
FOLDER = 'ReiksViewerListing'
out = os.path.join(parent, FOLDER)
SCREENSHOTS = ['1-video.png', '2-image-settings.png', '3-print.png', '4-options.png']
LICENSE = ('Reiks Viewer is free software under the MIT License. It includes libraries from the FFmpeg project '
           'under the GNU LGPL, version 3 or later; the source code of the included build is published with '
           'each release at https://github.com/reiks-apps/reiks-viewer/releases')

# The texts, language by language. Every language section in listing.md has the same six
# parts in the same order: short description, description, what's new, features, captions,
# search terms.
md = io.open(os.path.join(repo, 'store', 'listing.md'), encoding='utf-8').read()
sections = re.split(r'^## .*\((\w\w-\w\w)\)\s*$', md, flags=re.M)
texts = {}
for code, body in zip(sections[1::2], sections[2::2]):
    parts = re.split(r'^\*\*[^*]+\*\*\s*$', body, flags=re.M)[1:]
    assert len(parts) == 6, (code, len(parts))
    short, desc, news, feats, caps, terms = [p.strip().rstrip('-').strip() for p in parts]
    numbered = lambda block: [re.sub(r'^\d+\.\s*', '', l).strip() for l in block.splitlines() if re.match(r'^\d+\.', l)]
    t = texts[code] = {
        'ShortDescription': short,
        'Description': re.search(r'```\n(.*?)\n```', desc, re.S).group(1),
        'ReleaseNotes': news,
        'features': numbered(feats),
        'captions': numbered(caps),
        'terms': [term.strip() for term in terms.splitlines()[0].split('·')],
    }
    # The Store's limits.
    assert len(t['Description']) <= 10000 and len(t['features']) <= 20, code
    assert all(len(f) <= 200 for f in t['features'] + t['captions']), code
    assert len(t['terms']) <= 7 and all(len(term) <= 40 for term in t['terms']), code
    assert sum(len(term.split()) for term in t['terms']) <= 21, code
    assert len(t['captions']) == len(SCREENSHOTS), code

rows = list(csv.reader(io.StringIO(io.open(exported, encoding='utf-8-sig').read())))
column = {name: i for i, name in enumerate(rows[0])}
if os.path.exists(out):
    shutil.rmtree(out)
for code in texts:
    os.makedirs(os.path.join(out, 'screenshots', code))
    for name in SCREENSHOTS:
        shutil.copy(os.path.join(repo, 'dist', 'store-screenshots', code[:2], name), os.path.join(out, 'screenshots', code, name))
for logo in ('logo-300.png', 'logo-1080.png'):
    shutil.copy(os.path.join(repo, 'store', 'images', logo), os.path.join(out, logo))

def value(field, code, t):
    if field in ('ShortDescription', 'Description', 'ReleaseNotes'):
        return t[field]
    fixed = {
        'Title': 'Reiks Viewer',
        'DevStudio': 'Reiks Apps',
        'CopyrightTrademarkInformation': '© 2026 Reiks Apps',
        'AdditionalLicenseTerms': LICENSE,
        'StoreLogo300x300': f'{FOLDER}/logo-300.png',
        'StoreLogo1080x1080': f'{FOLDER}/logo-1080.png',
    }
    if field in fixed:
        return fixed[field]
    for pattern, items in ((r'DesktopScreenshot(\d+)', [f'{FOLDER}/screenshots/{code}/{n}' for n in SCREENSHOTS]),
                           (r'DesktopScreenshotCaption(\d+)', t['captions']),
                           (r'Feature(\d+)', t['features']),
                           (r'SearchTerm(\d+)', t['terms'])):
        if m := re.fullmatch(pattern, field):
            n = int(m.group(1))
            return items[n - 1] if n <= len(items) else None
    return None

for row in rows[1:]:
    for code, t in texts.items():
        v = value(row[0], code, t)
        if v is not None:
            row.extend([''] * (column[code] + 1 - len(row)))
            row[column[code]] = v

with io.open(os.path.join(out, 'listingData.csv'), 'w', encoding='utf-8-sig', newline='') as f:
    csv.writer(f, lineterminator='\r\n').writerows(rows)
print('wrote', out, 'for', ', '.join(texts))
