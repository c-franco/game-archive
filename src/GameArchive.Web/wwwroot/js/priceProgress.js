// priceProgress.js
// Blazor WASM cannot consume SSE natively, so we use fetch() streaming
// and forward events to .NET via DotNetObjectReference callbacks.

window.priceProgress = {
    _controller: null,

    start: async function (dotnetRef, url) {
        if (this._controller) this._controller.abort();
        this._controller = new AbortController();

        try {
            const response = await fetch(url, {
                method:  'POST',
                signal:  this._controller.signal,
                headers: { 'Accept': 'text/event-stream' }
            });

            if (!response.ok || !response.body) {
                dotnetRef.invokeMethodAsync('OnPriceError');
                return;
            }

            const reader  = response.body.getReader();
            const decoder = new TextDecoder();
            let   buffer  = '';

            while (true) {
                const { value, done } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });

                // SSE events are separated by \n\n
                const parts = buffer.split('\n\n');
                buffer = parts.pop(); // keep incomplete trailing chunk

                for (const part of parts) {
                    const line = part.trim();
                    if (!line.startsWith('data:')) continue;

                    let msg;
                    try { msg = JSON.parse(line.slice(5).trim()); }
                    catch { continue; }

                    if (msg.finished) {
                        dotnetRef.invokeMethodAsync(
                            'OnPriceDone',
                            msg.updated ?? 0,
                            msg.failed  ?? 0,
                            msg.errors  ?? []
                        );
                    } else {
                        dotnetRef.invokeMethodAsync(
                            'OnPriceProgress',
                            msg.done  ?? 0,
                            msg.total ?? 0
                        );
                    }
                }
            }
        } catch (err) {
            if (err.name !== 'AbortError') {
                dotnetRef.invokeMethodAsync('OnPriceError');
            }
        }
    },

    stop: function () {
        if (this._controller) {
            this._controller.abort();
            this._controller = null;
        }
    }
};
