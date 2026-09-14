(() => {
    const initialize = () => {
        const root = document.getElementById("podrska-list-container");
        if (!root || root.dataset.initialized === "true") return;
        root.dataset.initialized = "true";

        const text = JSON.parse(root.dataset.texts || "{}");
        const { escapeHtml, escapeAttribute } = window.adminCore;
        const $tableBody = $("#podrska-table-body");
        const $mobileList = $("#podrska-mobile-list");
        const $searchInput = $("#podrska-search-input");
        const $searchButton = $("#podrska-search-button");
        const pageParams = new URLSearchParams(window.location.search);
        let activeListRequest = null;
        const pager = window.adminLazyList.create({
            element: root.querySelector("[data-admin-lazy-controls]"),
            loadMoreText: text.loadMore,
            onLoadMore: (offset) => loadTickets(offset, true)
        });

        const emailLink = (value) => {
            const email = value || text.notAvailable;
            const safeEmail = escapeHtml(email);
            if (!value) return `<span class="admin-email-link is-empty">${safeEmail}</span>`;
            const safeAttribute = escapeAttribute(email);
            return `<a class="admin-email-link" href="mailto:${safeAttribute}" title="${safeAttribute}">${safeEmail}</a>`;
        };

        const formatDate = (dateString) => {
            if (!dateString) return "";
            const date = new Date(dateString);
            if (Number.isNaN(date.getTime())) return "";
            const day = String(date.getDate()).padStart(2, "0");
            const month = String(date.getMonth() + 1).padStart(2, "0");
            return `${day}-${month}-${date.getFullYear()}`;
        };

        const statusLabel = (status) => ({
            Poslat: text.statusSent,
            UObradi: text.statusInProgress,
            Odgovoren: text.statusAnswered,
            Zatvoren: text.statusClosed
        })[status] || status || text.notAvailable;

        const statusBadge = (status) => {
            const className = ({ Poslat: "info", UObradi: "warning", Odgovoren: "success", Zatvoren: "secondary" })[status] || "secondary";
            return `<span class="support-status-badge status-${className}">${escapeHtml(statusLabel(status))}</span>`;
        };

        const statusDropdown = (id, currentStatus) => {
            const statuses = [
                ["Poslat", text.statusSent],
                ["UObradi", text.statusInProgress],
                ["Odgovoren", text.statusAnswered],
                ["Zatvoren", text.statusClosed]
            ];
            const options = statuses.map(([value, label]) => `<li><a class="dropdown-item status-option ${currentStatus === value ? "active" : ""}" `
                + `href="#" data-status="${value}" data-upit-id="${id}">${escapeHtml(label)}</a></li>`).join("");
            return `<div class="dropdown status-dropdown"><button class="btn btn-sm btn-light border rounded-3 dropdown-toggle status-dropdown-toggle" `
                + `type="button" aria-expanded="false">${escapeHtml(text.status)}</button><ul class="dropdown-menu dropdown-menu-end">${options}</ul></div>`;
        };

        const replyHref = (ticket) => {
            const subject = `${text.replySubject} ${ticket.naslov || ""}`;
            const body = `${text.replyGreeting} ${ticket.korisnikIme || ""},\r\n\r\n${text.replyBody}\r\n\r\n`;
            return `mailto:${encodeURIComponent(ticket.korisnikEmail || "")}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
        };

        const ticketData = (ticket) => {
            const values = {
                "full-text": ticket.sadrzaj || "",
                email: ticket.korisnikEmail || text.notAvailable,
                title: ticket.naslov || "",
                date: formatDate(ticket.datumUpita),
                status: statusLabel(ticket.status),
                reply: replyHref(ticket)
            };
            return Object.entries(values)
                .map(([key, value]) => `data-${key}="${escapeAttribute(encodeURIComponent(value))}"`)
                .join(" ");
        };

        const viewButton = (ticket) => `<button class="btn btn-sm btn-outline-primary rounded-3 view-message-button" ${ticketData(ticket)}>`
            + `<i class="bi bi-eye me-1"></i>${escapeHtml(text.show)}</button>`;

        const actions = (ticket) => {
            const reply = replyHref(ticket);
            const item = `${text.inquiryPrefix} ${ticket.naslov || text.untitled} · ${ticket.korisnikEmail || text.notAvailable}`;
            return `<div class="table-actions support-actions-grid">${viewButton(ticket)}`
                + `<a href="${escapeAttribute(reply)}" class="btn btn-sm btn-outline-primary rounded-3"><i class="bi bi-reply me-1"></i>${escapeHtml(text.reply)}</a>`
                + statusDropdown(ticket.upitID, ticket.status)
                + `<button class="delete-podrska-button btn btn-sm btn-outline-danger rounded-3" data-id="${ticket.upitID}" data-confirm-delete="true" `
                + `data-confirm-title="${escapeAttribute(text.deleteTitle)}" data-confirm-message="${escapeAttribute(text.deleteMessage)}" `
                + `data-confirm-item="${escapeAttribute(item)}" data-confirm-action="${escapeAttribute(text.deleteAction)}">`
                + `<i class="bi bi-trash me-1"></i>${escapeHtml(text.delete)}</button></div>`;
        };

        const renderTickets = (tickets, append = false) => {
            if (!append) {
                $tableBody.empty();
                $mobileList.empty();
            }
            if (!tickets?.length) {
                if (append) return;
                const empty = `<div class="empty-state"><i class="bi bi-inbox"></i><p>${escapeHtml(text.empty)}</p></div>`;
                $tableBody.html(`<tr><td colspan="5">${empty}</td></tr>`);
                $mobileList.html(empty);
                return;
            }

            tickets.forEach((ticket) => {
                const date = escapeHtml(formatDate(ticket.datumUpita));
                const rawEmail = ticket.korisnikEmail || "";
                const email = emailLink(rawEmail);
                const rawTitle = ticket.naslov || text.untitled;
                const title = escapeHtml(rawTitle);
                const ticketActions = actions(ticket);

                $tableBody.append(`<tr data-admin-item-id="${ticket.upitID}"><td class="date-cell">${date}</td>`
                    + `<td><div class="user-cell" title="${escapeAttribute(rawEmail || text.notAvailable)}"><span class="user-avatar"><i class="bi bi-envelope-fill"></i></span><strong>${email}</strong></div></td>`
                    + `<td><strong class="table-text-truncate" title="${escapeAttribute(rawTitle)}">${title}</strong></td>`
                    + `<td>${statusBadge(ticket.status)}</td><td>${ticketActions}</td></tr>`);

                $mobileList.append(`<article class="support-mobile-card" data-admin-item-id="${ticket.upitID}"><div class="mobile-card-top"><span class="date-cell">${date}</span>${statusBadge(ticket.status)}</div>`
                    + `<h3>${title}</h3><p class="mobile-user">${email}</p><div class="review-mobile-meta">`
                    + `<div><span>${escapeHtml(text.status)}</span><strong data-support-status-label>${escapeHtml(statusLabel(ticket.status))}</strong></div>`
                    + `<div><span>${escapeHtml(text.customer)}</span><strong>${email}</strong></div></div>`
                    + `<div class="mobile-actions">${ticketActions}</div></article>`);
            });
        };

        const updateUrl = () => {
            const params = new URLSearchParams();
            params.set("section", "Podrska");
            if ($searchInput.val()) params.set("searchQuery", $searchInput.val());
            window.history.replaceState({}, "", `${window.location.pathname}?${params}`);
        };

        const loadTickets = (offset = 0, append = false) => {
            activeListRequest?.abort();
            if (append) {
                pager.setLoading(true);
            } else {
                pager.reset();
                window.adminCore.setLoading(root, true);
                updateUrl();
            }
            const request = window.appApi.get(root.dataset.listUrl, { searchQuery: $searchInput.val(), offset })
                .done((data) => {
                    const tickets = data?.upiti || [];
                    renderTickets(tickets, append);
                    pager.update(data, tickets.length, append);
                })
                .fail((xhr, statusText) => {
                    if (statusText === "abort") return;
                    if (append) {
                        window.showAppToast(window.adminCore.errorMessage(xhr, text.loadError), "error");
                        return;
                    }
                    const message = escapeHtml(window.adminCore.errorMessage(xhr, text.loadError));
                    const error = `<div class="alert alert-danger mb-0">${message}</div>`;
                    $tableBody.html(`<tr><td colspan="5">${error}</td></tr>`);
                    $mobileList.html(error);
                }).always(() => {
                    if (activeListRequest !== request) return;
                    activeListRequest = null;
                    pager.setLoading(false);
                    if (!append) window.adminCore.setLoading(root, false);
                });
            activeListRequest = request;
        };

        $(document).off("click.adminSupportToggle", ".status-dropdown-toggle")
            .on("click.adminSupportToggle", ".status-dropdown-toggle", function (event) {
                event.preventDefault();
                event.stopPropagation();
                bootstrap.Dropdown.getOrCreateInstance(this, {
                    boundary: document.body,
                    popperConfig: (config) => ({
                        ...config,
                        strategy: "fixed",
                        modifiers: [
                            ...(config.modifiers || []),
                            { name: "preventOverflow", options: { boundary: "viewport", rootBoundary: "viewport" } },
                            { name: "flip", options: { boundary: "viewport", rootBoundary: "viewport" } }
                        ]
                    })
                }).toggle();
            });

        $(document).off("click.adminSupportView", ".view-message-button")
            .on("click.adminSupportView", ".view-message-button", function () {
                const value = (name, fallback = "-") => decodeURIComponent($(this).attr(`data-${name}`) || fallback);
                $("#messageModalBody").text(value("full-text", ""));
                $("#messageModalEmail").text(value("email"));
                $("#messageModalTitle").text(value("title"));
                $("#messageModalDate").text(value("date"));
                $("#messageModalStatus").text(value("status"));
                $("#messageModalReply").attr("href", value("reply", "#"));
                bootstrap.Modal.getOrCreateInstance(document.getElementById("messageModal")).show();
            });

        $(document).off("click.adminSupportStatus", ".status-option")
            .on("click.adminSupportStatus", ".status-option", function (event) {
                event.preventDefault();
                window.appApi.post(root.dataset.statusUrl, {
                    id: $(this).data("upit-id"),
                    status: $(this).data("status")
                }).done((response) => {
                    if (response.successMessage) window.showAppToast(response.successMessage, "success");
                    const id = String($(this).data("upit-id"));
                    const status = String($(this).data("status"));
                    const label = statusLabel(status);
                    const $items = $(root).find(`[data-admin-item-id="${CSS.escape(id)}"]`);
                    $items.find(".support-status-badge").replaceWith(statusBadge(status));
                    $items.find("[data-support-status-label]").text(label);
                    $items.find(".status-option").removeClass("active")
                        .filter(`[data-status="${CSS.escape(status)}"]`).addClass("active");
                    $items.find(".view-message-button").attr("data-status", encodeURIComponent(label));
                }).fail(() => window.showAppToast(text.statusChangeError, "error"));
            });

        $(document).off("app:delete-confirmed.adminSupport", ".delete-podrska-button")
            .on("app:delete-confirmed.adminSupport", ".delete-podrska-button", function () {
                const id = $(this).data("id");
                window.appApi.post(root.dataset.deleteUrl, { id }).done((response) => {
                    window.showAppToast(response.successMessage || text.deleteSuccess, "success");
                    window.adminCore.removeRenderedItem(root, id, () => renderTickets([]));
                    pager.removeItem();
                }).fail(() => window.showAppToast(text.deleteError, "error"));
            });

        $searchButton.off("click.adminSupport").on("click.adminSupport", loadTickets);
        $searchInput.off("keydown.adminSupport").on("keydown.adminSupport", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                loadTickets();
            }
        });

        if (!$searchInput.val()) {
            $searchInput.val(pageParams.get("section") === "Podrska"
                ? pageParams.get("searchQuery") || root.dataset.initialSearch
                : root.dataset.initialSearch);
        }
        loadTickets();
    };

    $(document).on("admin:section-loaded.adminSupport", (_, sectionName) => {
        if (sectionName === "Podrska") initialize();
    });
})();
