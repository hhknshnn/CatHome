"""Fetch public CC0 cat recordings and preserve their source/license evidence."""
from pathlib import Path
import hashlib, json, re, urllib.request

ROOT = Path(__file__).resolve().parent
SOURCES = [
    ("eat", "indieground", 238297),
    ("drink", "16H_Panska_Rudenko_Angelika", 499358),
    ("drink_alt", "16GPanskaZlochova_Eliska", 496277),
]


def sanitize_source_html(page):
    """Keep audio/license evidence without archiving third-party Mapbox tokens."""
    return re.sub(
        r"(?<![A-Za-z0-9_])(?:sk|pk)\.[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{10,}",
        "[REDACTED_MAPBOX_TOKEN]",
        page,
    )


for name, author, sound_id in SOURCES:
    url = f"https://freesound.org/people/{author}/sounds/{sound_id}/"
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=30) as response:
        page = response.read().decode("utf-8")
    (ROOT / f"{name}-source.html").write_text(sanitize_source_html(page), encoding="utf-8")
    assert "creativecommons.org/publicdomain/zero" in page, "CC0 must be confirmed on the source page"
    links = list(dict.fromkeys(re.findall(r'https://[^\s\"<>]+(?:-hq\.mp3|-hq\.ogg)', page)))
    print(json.dumps({"name": name, "source": url, "license": "CC0-1.0", "previews": links}), flush=True)
    assert links, "No public preview found"
    media_url = next((s for s in links if s.endswith("-hq.mp3")), links[0])
    with urllib.request.urlopen(urllib.request.Request(media_url, headers={"User-Agent": "Mozilla/5.0"}), timeout=30) as response:
        data = response.read()
    path = ROOT / (name + Path(media_url).suffix)
    path.write_bytes(data)
    (ROOT / f"{name}-source.json").write_text(json.dumps({
        "title": name, "author": author, "sound_id": sound_id, "source": url,
        "license": "CC0-1.0", "license_url": "https://creativecommons.org/publicdomain/zero/1.0/",
        "download": media_url, "file": path.name, "sha256": hashlib.sha256(data).hexdigest(),
        "retrieved": "2026-09-15", "format_note": "Public high-quality preview from the original sound page"
    }, indent=2), encoding="utf-8")
