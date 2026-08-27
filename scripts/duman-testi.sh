#!/usr/bin/env bash
# Dagitim sonrasi saglik kontrolu.
#
#   ./duman-testi.sh <temel-adres> <ortam>      ortam: test | canli
#
# Bu haftaki olaylardan cikarilmis kontroller. "200 dondu" yeterli degil —
# canlida site "kayitli dukkan bulunmuyor" gosterirken de 200 donuyordu.

set -uo pipefail

URL="${1:?temel adres gerekli}"
ORTAM="${2:?ortam gerekli (test|canli)}"

# Linux App Service'te ilk acilis uzun surebiliyor; ilk dagitimda
# 3-4 dakika gorulmustu. Pencere ona gore.
DENEME=15
ARALIK=20

gecti=0
kaldi=0

# ── Cloudflare Access servis jetonu ─────────────────────────────────────────
# Test API'si Access arkasinda. Jetonsuz istekler giris sayfasina 302 ile
# yonlendiriliyor, yani "uc calisiyor mu" sorusu hic sorulamiyor. Access
# devreye girdiginden beri dagitim bu yuzden kirmiziydi.
#
# Herkese acik uclar (saglik kontrolu) jeton olmadan da yanit veriyor cunku
# Access yalnizca /test/* yoluna uygulanmis durumda; yine de tum isteklere
# ekliyoruz — kapsam genisletilirse burasi kendiliginden calismaya devam eder.
KIMLIK=()
if [ -n "${CF_ACCESS_CLIENT_ID:-}" ] && [ -n "${CF_ACCESS_CLIENT_SECRET:-}" ]; then
  KIMLIK=(-H "CF-Access-Client-Id: ${CF_ACCESS_CLIENT_ID}"
          -H "CF-Access-Client-Secret: ${CF_ACCESS_CLIENT_SECRET}")
fi

sonuc() {
  if [ "$1" = "ok" ]; then
    gecti=$((gecti + 1)); echo "  ✓ $2"
  else
    kaldi=$((kaldi + 1)); echo "  ✗ $2"
  fi
}

# ── 1. Uygulama ayaga kalkti mi ─────────────────────────────────────────────
echo "1) Saglik (en fazla $((DENEME * ARALIK / 60)) dk bekleniyor)"

kod=000
for i in $(seq 1 "$DENEME"); do
  kod=$(curl -s -o /tmp/duman.json -w '%{http_code}' --max-time 45 \
    "${KIMLIK[@]}" \
    "$URL/api/Shop/public/all?pageNumber=1&pageSize=5") || kod=000
  echo "   deneme $i: HTTP $kod"
  [ "$kod" = "200" ] && break
  sleep "$ARALIK"
done

if [ "$kod" != "200" ]; then
  echo "::error::$ORTAM ortami saglikli yanit vermedi (son kod: $kod)."
  exit 1
fi
sonuc ok "API yanit veriyor"

# ── 2. Yanit gercekten JSON mu ──────────────────────────────────────────────
# 200 donen bir hata sayfasi da 200'dur; icerigi dogrulamak gerekiyor.
if python3 -c "import json,sys; json.load(open('/tmp/duman.json'))" 2>/dev/null; then
  sonuc ok "Yanit gecerli JSON"
else
  sonuc fail "Yanit JSON degil — icerik: $(head -c 120 /tmp/duman.json)"
fi

# ── 3. Medya adresleri CDN'i gosteriyor mu ──────────────────────────────────
# Ham anahtar donmesi, MediaUrlConverter'in devre disi kaldigi anlamina gelir:
# tum gorseller kirik gorunur ama API yine 200 doner.
ham=$(grep -oE '"[a-zA-Z]*(Url|Path)"\s*:\s*"(shops|ads|reviews|profile_images)/[^"]+"' /tmp/duman.json | wc -l)
if [ "$ham" -eq 0 ]; then
  sonuc ok "Ham medya anahtari yok"
else
  sonuc fail "$ham adet tamamlanmamis medya anahtari — Media__BaseUrl eksik olabilir"
fi

# ── 4. Test uclari dogru ortamda mi ─────────────────────────────────────────
# Canlida OTP'nin ekrandan okunabilir olmasi ciddi bir guvenlik acigi olurdu.
#
# Canli tarafta jeton BILEREK gonderilmiyor: orada sorulan soru "bu uc kapali
# mi", ve buna kimliksiz bir istekle cevap vermek gerekiyor. Jetonla 404
# gormek, ucun herkese kapali oldugunu kanitlamaz.
if [ "$ORTAM" = "canli" ]; then
  otp=$(curl -s -o /dev/null -w '%{http_code}' --max-time 30 "$URL/test/otp/json")
  [ "$otp" = "404" ] \
    && sonuc ok "Test uclari canlida kapali (404)" \
    || sonuc fail "TEST UCU CANLIDA ACIK (HTTP $otp) — derhal mudahale"
else
  otp=$(curl -s -o /dev/null -w '%{http_code}' --max-time 30 "${KIMLIK[@]}" "$URL/test/otp/json")
  [ "$otp" = "200" ] \
    && sonuc ok "OTP yakalama ucu calisiyor" \
    || sonuc fail "OTP yakalama ucu yanit vermiyor (HTTP $otp)"

  # APK listesi de ayni ortam korumasina bagli; kova bos olsa bile 200
  # donmeli. Ilk surumde bos kovada 500 donuyordu (S3 null liste).
  apk=$(curl -s -o /dev/null -w '%{http_code}' --max-time 30 "${KIMLIK[@]}" "$URL/test/apk/json")
  [ "$apk" = "200" ] \
    && sonuc ok "APK listesi calisiyor" \
    || sonuc fail "APK listesi yanit vermiyor (HTTP $apk)"
fi

# ── Sonuc ───────────────────────────────────────────────────────────────────
echo
echo "gecti: $gecti   kaldi: $kaldi"
[ "$kaldi" -eq 0 ] || { echo "::error::$ORTAM duman testi basarisiz."; exit 1; }
