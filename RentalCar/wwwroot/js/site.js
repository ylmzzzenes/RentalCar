(function () {
    const widget = document.getElementById("ai-chat-widget");
    if (!widget) return;

    const panel = document.getElementById("ai-chat-panel");
    const toggle = document.getElementById("ai-chat-toggle");
    const close = document.getElementById("ai-chat-close");
    const form = document.getElementById("ai-chat-form");
    const input = document.getElementById("ai-chat-input");
    const messages = document.getElementById("ai-chat-messages");
    const cards = document.getElementById("ai-chat-cars");
    const applyFilters = document.getElementById("ai-apply-filters");
    const tokenInput = form ? form.querySelector('input[name="__RequestVerificationToken"]') : null;

    let latestFilters = null;

    toggle?.addEventListener("click", function () {
        panel?.classList.toggle("d-none");
        if (panel && !panel.classList.contains("d-none")) {
            input?.focus();
        }
    });

    close?.addEventListener("click", function () {
        panel?.classList.add("d-none");
    });

    form?.addEventListener("submit", async function (event) {
        event.preventDefault();
        const text = (input?.value || "").trim();
        if (!text) return;

        appendBubble(text, true);
        input.value = "";
        clearCards();
        setLoading(true);

        try {
            const response = await fetch("/chat/message", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenInput ? tokenInput.value : ""
                },
                body: JSON.stringify({ message: text })
            });

            const data = await response.json();
            if (!response.ok) {
                appendBubble(data?.error || "Asistan yanit veremedi.", false);
                return;
            }

            appendBubble(data.message || "Yanit alinamadi.", false);
            if (Array.isArray(data.cars) && data.cars.length > 0) {
                renderCars(data.cars);
            }

            latestFilters = data.suggestedFilters || null;
            applyFilters?.classList.toggle("d-none", !latestFilters);
        } catch {
            appendBubble("Baglanti hatasi olustu. Lutfen tekrar deneyin.", false);
        } finally {
            setLoading(false);
        }
    });

    applyFilters?.addEventListener("click", function () {
        if (!latestFilters) return;
        const url = new URL(window.location.origin + "/Car/List");
        if (latestFilters.city) url.searchParams.set("searchString", latestFilters.city);
        if (latestFilters.fuelType) url.searchParams.set("yakitTuru", latestFilters.fuelType);
        window.location.href = url.toString();
    });

    function appendBubble(text, isUser) {
        if (!messages) return;
        const div = document.createElement("div");
        div.className = "ai-bubble " + (isUser ? "ai-bubble-user" : "ai-bubble-assistant");
        div.textContent = text;
        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
    }

    function renderCars(items) {
        if (!cards) return;
        cards.innerHTML = "";
        for (const item of items) {
            const image = item.imageUrl || "https://via.placeholder.com/112x112?text=Arac";
            const title = item.title || "Arac";
            const price = item.price ? Number(item.price).toLocaleString("tr-TR") : "-";
            const city = item.city || "-";
            const html = `
                <a class="ai-car-card text-decoration-none text-light" href="/Car/Details/${item.carId}">
                    <img src="${image}" alt="${escapeHtml(title)}" onerror="this.src='https://via.placeholder.com/112x112?text=Arac'" />
                    <div>
                        <div class="fw-semibold">${escapeHtml(title)}</div>
                        <small class="text-secondary">${escapeHtml(city)} - ${price} TL/gun</small>
                    </div>
                </a>`;
            cards.insertAdjacentHTML("beforeend", html);
        }
    }

    function clearCards() {
        if (cards) cards.innerHTML = "";
        applyFilters?.classList.add("d-none");
        latestFilters = null;
    }

    function setLoading(isLoading) {
        if (!input) return;
        input.disabled = isLoading;
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }
})();
