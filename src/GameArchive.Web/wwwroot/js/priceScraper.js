// priceScraper.js
// All HTTP requests to PriceCharting happen HERE, in the browser,
// so they carry real cookies/headers and avoid server-side bot detection.

window.priceScraper = {

    // ── Slug map: platform|region → PriceCharting console slug ───────────────
    _slugMap: {
        "nes|pal": "pal-nes",
        "snes|pal": "pal-super-nintendo",
        "nintendo 64|pal": "pal-nintendo-64",
        "gamecube|pal": "pal-gamecube",
        "wii|pal": "pal-wii",
        "wii u|pal": "pal-wii-u",
        "nintendo switch|pal": "pal-nintendo-switch",
        "nintendo switch 2|pal": "pal-nintendo-switch-2",
        "game boy|pal": "pal-gameboy",
        "game boy color|pal": "pal-gameboy-color",
        "game boy advance|pal": "pal-gameboy-advance",
        "nintendo ds|pal": "pal-nintendo-ds",
        "nintendo 3ds|pal": "pal-nintendo-3ds",
        "nes|ntsc-u": "nes",
        "snes|ntsc-u": "super-nintendo",
        "nintendo 64|ntsc-u": "nintendo-64",
        "gamecube|ntsc-u": "gamecube",
        "wii|ntsc-u": "wii",
        "wii u|ntsc-u": "wii-u",
        "nintendo switch|ntsc-u": "nintendo-switch",
        "nintendo switch 2|ntsc-u": "nintendo-switch-2",
        "game boy|ntsc-u": "gameboy",
        "game boy color|ntsc-u": "gameboy-color",
        "game boy advance|ntsc-u": "gameboy-advance",
        "nintendo ds|ntsc-u": "nintendo-ds",
        "nintendo 3ds|ntsc-u": "nintendo-3ds",
        "nes|ntsc-j": "famicom",
        "snes|ntsc-j": "super-famicom",
        "nintendo 64|ntsc-j": "jp-nintendo-64",
        "gamecube|ntsc-j": "jp-gamecube",
        "game boy|ntsc-j": "jp-gameboy",
        "game boy color|ntsc-j": "jp-gameboy-color",
        "game boy advance|ntsc-j": "jp-gameboy-advance",
        "nintendo ds|ntsc-j": "jp-nintendo-ds",
        "nintendo 3ds|ntsc-j": "jp-nintendo-3ds",
        "playstation|pal": "pal-playstation",
        "playstation 2|pal": "pal-ps2",
        "playstation 3|pal": "pal-ps3",
        "playstation 4|pal": "pal-ps4",
        "psp|pal": "pal-psp",
        "ps vita|pal": "pal-ps-vita",
        "playstation|ntsc-u": "playstation",
        "playstation 2|ntsc-u": "playstation-2",
        "playstation 3|ntsc-u": "playstation-3",
        "playstation 4|ntsc-u": "playstation-4",
        "playstation 5|ntsc-u": "playstation-5",
        "psp|ntsc-u": "psp",
        "ps vita|ntsc-u": "ps-vita",
        "playstation|ntsc-j": "jp-playstation",
        "playstation 2|ntsc-j": "jp-ps2",
        "xbox|pal": "pal-xbox",
        "xbox 360|pal": "pal-xbox-360",
        "xbox|ntsc-u": "xbox",
        "xbox 360|ntsc-u": "xbox-360",
        "xbox one|ntsc-u": "xbox-one",
        "xbox series x/s|ntsc-u": "xbox-series-x",
        "sega mega drive|pal": "pal-sega-genesis",
        "sega saturn|pal": "pal-sega-saturn",
        "sega dreamcast|pal": "pal-sega-dreamcast",
        "sega mega drive|ntsc-u": "sega-genesis",
        "sega saturn|ntsc-u": "sega-saturn",
        "sega dreamcast|ntsc-u": "sega-dreamcast",
    },

    resolveSlug(platform, region) {
        let r = region.trim().toUpperCase();
        if (r === "NTSC") r = "NTSC-U";
        const key = `${platform.trim().toLowerCase()}|${r.toLowerCase()}`;
        return this._slugMap[key] ?? null;
    },

    resolveCondition(item) {
        if ((item.condition || "").toLowerCase() === "new") return "New";
        const checked = (item.checklistEntries || [])
            .filter(e => e.isChecked)
            .map(e => e.label.trim().toLowerCase());
        const hasBox = checked.some(l => l.includes("box") || l.includes("caja"));
        const hasManual = checked.some(l => l.includes("manual"));

        if (item.type === "Console") {
            // For consoles: CIB = Box/Caja + Controller/Mando + Cables
            const hasController = checked.some(l => l.includes("controller") || l.includes("mando"));
            const hasCables = checked.some(l => l.includes("cable"));
            return (hasBox && hasController && hasCables) ? "CIB" : "Loose";
        } else {
            // For games: CIB = Box/Caja + Manual + Cartridge/Disc/Cartucho/Disco
            const hasMedia = checked.some(l =>
                l.includes("cartridge") || l.includes("disc") ||
                l.includes("disk")      || l.includes("game") ||
                l.includes("cartucho")  || l.includes("disco"));
            return (hasBox && hasManual && hasMedia) ? "CIB" : "Loose";
        }
    },

    // Build name slug: lowercase, hyphens, strip special chars
    _nameSlug(name) {
        return name.trim().toLowerCase()
            .replace(/[''']/g, "")
            .replace(/[^a-z0-9\s-]/g, " ")
            .replace(/\s+/g, "-")
            .replace(/^-+|-+$/g, "");
    },

    // Fetch HTML via a CORS proxy since PriceCharting doesn't allow cross-origin
    async _fetchHtml(url) {
        // Use allorigins.win as a free CORS proxy
        const proxyUrl = `https://api.allorigins.win/get?url=${encodeURIComponent(url)}`;
        const resp = await fetch(proxyUrl, { signal: AbortSignal.timeout(15000) });
        if (!resp.ok) throw new Error(`Proxy error ${resp.status}`);
        const json = await resp.json();
        return json.contents ?? "";
    },

    // Extract price from HTML by element id
    // PriceCharting uses: <span id="used-price" ...>$12.34</span>
    // or data-price="1234" (cents)
    _extractPrice(html, id) {
        // Try data-price attribute (integer cents)
        const dpRe = new RegExp(`id="${id}"[^>]*data-price="(\\d+)"`, "i");
        const dpM  = html.match(dpRe);
        if (dpM) return parseInt(dpM[1], 10) / 100;

        // Try dollar sign in text after the id
        const txtRe = new RegExp(`id="${id}"[^>]*>[\\s\\S]{0,300}?\\$(\\d[\\d,]*\\.?\\d{0,2})`, "i");
        const txtM  = html.match(txtRe);
        if (txtM) return parseFloat(txtM[1].replace(/,/g, ""));

        return null;
    },

    async _scrapePrices(productUrl) {
        const html = await this._fetchHtml(productUrl);
        return {
            loose:    this._extractPrice(html, "used-price"),
            cib:      this._extractPrice(html, "complete-price"),
            newPrice: this._extractPrice(html, "new-price"),
            html,     // returned for debug if needed
        };
    },

    async _findProductUrl(name, consoleSlug) {
        // 1. Try direct URL
        const directUrl = `https://www.pricecharting.com/game/${consoleSlug}/${this._nameSlug(name)}`;
        try {
            const html = await this._fetchHtml(directUrl);
            if (html.includes("used-price") || html.includes("complete-price")) {
                return directUrl;
            }
        } catch (_) { /* fall through */ }

        // 2. Search
        const searchUrl = `https://www.pricecharting.com/search-products?type=prices&q=${encodeURIComponent(name)}`;
        let searchHtml;
        try {
            searchHtml = await this._fetchHtml(searchUrl);
        } catch (e) {
            throw new Error(`Search request failed: ${e.message}`);
        }

        // Find /game/... links, prefer ones containing the console slug
        const linkRe = /href="(\/game\/[^"?#]+)"/gi;
        const candidates = [];
        let m;
        while ((m = linkRe.exec(searchHtml)) !== null) {
            candidates.push(m[1]);
        }

        const slugMatch = candidates.find(h => h.includes(consoleSlug));
        if (slugMatch) return "https://www.pricecharting.com" + slugMatch;
        if (candidates.length > 0) return "https://www.pricecharting.com" + candidates[0];
        return null;
    },

    async _getEurRate() {
        try {
            const r = await fetch("https://api.frankfurter.app/latest?from=USD&to=EUR",
                { signal: AbortSignal.timeout(5000) });
            const j = await r.json();
            return j.rates.EUR;
        } catch (_) {
            return 0.92;
        }
    },

    // ── Main entry: fetch price for one item ──────────────────────────────────
    async fetchPrice(item) {
        const slug = this.resolveSlug(item.platform, item.region);
        if (!slug) {
            return { success: false, error: `Plataforma/región no soportada: '${item.platform}' / '${item.region}'` };
        }

        const condition = this.resolveCondition(item);

        let productUrl;
        try {
            productUrl = await this._findProductUrl(item.name, slug);
        } catch (e) {
            return { success: false, error: `Error en búsqueda: ${e.message}` };
        }

        if (!productUrl) {
            return { success: false, error: "Producto no encontrado en PriceCharting" };
        }

        let prices;
        try {
            prices = await this._scrapePrices(productUrl);
        } catch (e) {
            return { success: false, error: `Error al obtener precio: ${e.message}` };
        }

        const priceUsd = condition === "New"  ? (prices.newPrice ?? prices.cib ?? prices.loose)
                       : condition === "CIB"  ? (prices.cib     ?? prices.loose)
                       :                        (prices.loose    ?? prices.cib);

        if (priceUsd == null) {
            return { success: false, productUrl, error: "Precio no disponible en la página" };
        }

        const rate     = await this._getEurRate();
        const priceEur = Math.round(priceUsd * rate * 100) / 100;

        return { success: true, priceEur, condition, productUrl };
    },

    // ── Batch: process all items, report progress to Blazor ──────────────────
    async fetchAll(items, dotnetRef) {
        const total   = items.length;
        let updated   = 0;
        let failed    = 0;
        const errors  = [];

        for (let i = 0; i < items.length; i++) {
            const item   = items[i];
            const result = await this.fetchPrice(item);

            if (result.success) {
                // Save price back to server
                try {
                    await fetch(`/api/price/save/${item.id}`, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({
                            estimatedValue: result.priceEur,
                            priceSource: "PriceCharting",
                            productUrl: result.productUrl
                        })
                    });
                    updated++;
                } catch (e) {
                    failed++;
                    errors.push(`${item.name}: Error al guardar (${e.message})`);
                }
            } else {
                failed++;
                errors.push(`${item.name}: ${result.error}`);
            }

            // Report progress to Blazor
            dotnetRef.invokeMethodAsync("OnPriceProgress", i + 1, total, updated, failed);

            // Polite delay between requests
            if (i < items.length - 1) {
                await new Promise(r => setTimeout(r, 1500));
            }
        }

        dotnetRef.invokeMethodAsync("OnPriceDone", updated, failed, errors);
    }
};
