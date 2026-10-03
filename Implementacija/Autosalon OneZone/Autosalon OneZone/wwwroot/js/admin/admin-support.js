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
        const $queueChips = $(root).find(".support-queue-chip");
        const currentUserId = root.dataset.currentUserId || "";
        const isAdministrator = root.dataset.isAdministrator === "true";
        const pageParams = new URLSearchParams(window.location.search);
        const validQueues = $queueChips.map((_, chip) => chip.dataset.queueFilter).get();
        let queueFilter = "new";
        let currentDirection = "desc";
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById("messageModal"));
        let activeListRequest = null;
        let activeConversation = null;
        const pager = window.adminLazyList.create({
            element: root.querySelector("[data-admin-lazy-controls]"),
            loadMoreText: text.loadMore,
            onLoadMore: (offset) => loadTickets(offset, true)
        });

        const formatDate = (dateString, includeTime = false) => {
            if (!dateString) return "";
            const date = new Date(dateString);
            if (Number.isNaN(date.getTime())) return "";
            const day = String(date.getDate()).padStart(2, "0");
            const month = String(date.getMonth() + 1).padStart(2, "0");
            const datePart = `${day}-${month}-${date.getFullYear()}`;
            return includeTime
                ? `${datePart} ${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`
                : datePart;
        };

        const statusLabel = (status) => ({
            CekaPodrsku: text.statusWaitingSupport,
            UObradi: text.statusInProgress,
            CekaKorisnika: text.statusAnswered,
            Rijesen: text.statusAnswered,
            Zatvoren: text.statusClosed
        })[status] || status || text.notAvailable;

        const statusBadge = (status) => {
            const className = ({ CekaPodrsku: "info", UObradi: "warning", CekaKorisnika: "waiting", Rijesen: "waiting", Zatvoren: "secondary" })[status] || "secondary";
            return `<span class="support-status-badge status-${className}">${escapeHtml(statusLabel(status))}</span>`;
        };

        const emailLink = (value) => {
            const email = value || text.notAvailable;
            if (!value) return `<span class="admin-email-link is-empty">${escapeHtml(email)}</span>`;
            return `<a class="admin-email-link" href="mailto:${escapeAttribute(value)}" title="${escapeAttribute(value)}">${escapeHtml(email)}</a>`;
        };

        const viewButton = (ticket) => `<button class="btn btn-sm btn-outline-primary rounded-3 view-message-button" data-id="${ticket.upitID}">`
            + `<i class="bi bi-eye me-1"></i>${escapeHtml(text.show)}`
            + `${ticket.neprocitano > 0 ? `<span class="support-action-count">${ticket.neprocitano}</span>` : ""}</button>`;

        const actions = (ticket) => {
            const item = `${text.inquiryPrefix} ${ticket.naslov || text.untitled} · ${ticket.korisnikEmail || text.notAvailable}`;
            const deleteButton = isAdministrator
                ? `<button class="delete-podrska-button btn btn-sm btn-outline-danger rounded-3" data-id="${ticket.upitID}" data-confirm-delete="true" `
                    + `data-confirm-title="${escapeAttribute(text.deleteTitle)}" data-confirm-message="${escapeAttribute(text.deleteMessage)}" `
                    + `data-confirm-item="${escapeAttribute(item)}" data-confirm-action="${escapeAttribute(text.deleteAction)}">`
                    + `<i class="bi bi-trash me-1"></i>${escapeHtml(text.delete)}</button>`
                : "";
            return `<div class="table-actions support-actions-grid">${viewButton(ticket)}${deleteButton}</div>`;
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
                const email = emailLink(ticket.korisnikEmail || "");
                const title = escapeHtml(ticket.naslov || text.untitled);
                const assigned = ticket.dodijeljenKorisnikIme
                    ? `<span class="support-assignee"><i class="bi bi-person-check-fill"></i>${escapeHtml(ticket.dodijeljenKorisnikIme)}</span>`
                    : `<span class="support-assignee is-empty">${escapeHtml(text.unassigned)}</span>`;
                const ticketActions = actions(ticket);

                $tableBody.append(`<tr data-admin-item-id="${ticket.upitID}"><td class="date-cell">${date}</td>`
                    + `<td><div class="user-cell" title="${escapeAttribute(ticket.korisnikEmail || text.notAvailable)}"><span class="user-avatar"><i class="bi bi-envelope-fill"></i></span><strong>${email}</strong></div></td>`
                    + `<td><strong class="table-text-truncate" title="${escapeAttribute(ticket.naslov || text.untitled)}">${title}</strong>${assigned}</td>`
                    + `<td>${statusBadge(ticket.status)}</td><td>${ticketActions}</td></tr>`);

                $mobileList.append(`<article class="support-mobile-card" data-admin-item-id="${ticket.upitID}"><div class="mobile-card-top"><span class="date-cell">${date}</span>${statusBadge(ticket.status)}</div>`
                    + `<h3>${title}</h3><p class="mobile-user">${email}</p><div class="review-mobile-meta">`
                    + `<div><span>${escapeHtml(text.status)}</span><strong>${escapeHtml(statusLabel(ticket.status))}</strong></div>`
                    + `<div><span>${escapeHtml(text.assignedTo)}</span><strong>${assigned}</strong></div></div>`
                    + `<div class="mobile-actions">${ticketActions}</div></article>`);
            });
        };

        const updateUrl = () => {
            const params = new URLSearchParams();
            params.set("section", "Podrska");
            if ($searchInput.val()) params.set("searchQuery", $searchInput.val());
            params.set("queueFilter", queueFilter);
            params.set("sort", "datum");
            params.set("direction", currentDirection);
            window.history.replaceState({}, "", `${window.location.pathname}?${params}`);
            updateFilterControls();
        };

        const updateFilterControls = () => {
            $queueChips.removeClass("active")
                .filter(`[data-queue-filter="${CSS.escape(queueFilter)}"]`).addClass("active");
            $(root).find(".support-sort-link i")
                .removeClass("bi-arrow-down-short bi-arrow-up-short")
                .addClass(currentDirection === "asc" ? "bi-arrow-up-short" : "bi-arrow-down-short");
        };

        const loadTickets = (offset = 0, append = false) => {
            activeListRequest?.abort();
            if (append) pager.setLoading(true);
            else {
                pager.reset();
                window.adminCore.setLoading(root, true);
                updateUrl();
            }
            const request = window.appApi.get(root.dataset.listUrl, {
                searchQuery: $searchInput.val(),
                queueFilter,
                direction: currentDirection,
                offset
            })
                .done((data) => {
                    const tickets = data?.upiti || [];
                    renderTickets(tickets, append);
                    pager.update(data, tickets.length, append);
                })
                .fail((xhr, statusText) => {
                    if (statusText === "abort") return;
                    const message = window.adminCore.errorMessage(xhr, text.loadError);
                    if (append) window.showAppToast(message, "error");
                    else {
                        const error = `<div class="alert alert-danger mb-0">${escapeHtml(message)}</div>`;
                        $tableBody.html(`<tr><td colspan="5">${error}</td></tr>`);
                        $mobileList.html(error);
                    }
                })
                .always(() => {
                    if (activeListRequest !== request) return;
                    activeListRequest = null;
                    pager.setLoading(false);
                    if (!append) window.adminCore.setLoading(root, false);
                });
            activeListRequest = request;
        };

        const renderConversation = (conversation) => {
            activeConversation = conversation;
            $("#messageModalTitle").text(conversation.title || text.untitled);
            $("#messageModalCustomer").text(conversation.customerName || conversation.customerEmail);
            $("#messageModalEmail").text(conversation.customerEmail || text.notAvailable);
            $("#messageModalStatus").text(statusLabel(conversation.status));
            $("#messageModalAssignee").text(conversation.assignedAgentName || text.unassigned);

            const messages = (conversation.messages || []).map((message) => {
                const type = message.senderType === 0 || message.senderType === "Korisnik"
                    ? "customer"
                    : message.senderType === 2 || message.senderType === "Sistem" ? "system" : "staff";
                if (type === "system") {
                    const systemText = ({
                        SupportSystemReopened: text.systemReopened,
                        SupportSystemClosed: text.systemClosed,
                        SupportSystemReminder: text.systemReminder,
                        SupportSystemAutoClosed: text.systemAutoClosed
                    })[message.content] || message.content;
                    return `<div class="admin-support-message is-system"><i class="bi bi-info-circle"></i>${escapeHtml(systemText)}</div>`;
                }
                return `<article class="admin-support-message is-${type}"><div><strong>${escapeHtml(message.senderName)}</strong>`
                    + `<time>${escapeHtml(formatDate(message.sentUtc, true))}</time></div><p>${escapeHtml(message.content)}</p></article>`;
            }).join("");
            $("#messageModalThread").html(messages);
            $("#messageModalReplyError").addClass("d-none");
            $("#messageModalReplyText").removeClass("is-invalid").removeAttr("aria-invalid");

            const assignedToMe = conversation.assignedAgentId === currentUserId;
            const assignedToOther = Boolean(conversation.assignedAgentId) && !assignedToMe;
            const closed = conversation.status === "Zatvoren" || conversation.status === 4;
            $("#messageModalTake")
                .toggle(!closed && !assignedToMe && (!assignedToOther || isAdministrator))
                .html(`<i class="bi bi-person-check me-2"></i>${escapeHtml(assignedToOther ? text.takeOver : text.take)}`);
            $("#messageModalRelease").toggle(!closed && Boolean(conversation.assignedAgentId) && (assignedToMe || isAdministrator));
            $("#messageModalClose").toggle(!closed && Boolean(conversation.assignedAgentId) && (assignedToMe || isAdministrator));
            $("#messageModalComposer").toggle(!closed && assignedToMe);
            $("#messageModalReplyText").val("");
        };

        const refreshConversation = (response) => {
            if (response?.conversation) renderConversation(response.conversation);
            if (response?.successMessage) window.showAppToast(response.successMessage, "success");
            loadTickets();
        };

        const postConversationAction = (url, data) => window.appApi.post(url, data)
            .done(refreshConversation)
            .fail((xhr) => {
                window.showAppToast(window.adminCore.errorMessage(xhr, text.loadError), "error");
                if (xhr.responseJSON?.conversation) renderConversation(xhr.responseJSON.conversation);
            });

        $(document).off("click.adminSupportView", ".view-message-button")
            .on("click.adminSupportView", ".view-message-button", function () {
                window.appApi.get(root.dataset.detailsUrl, { id: $(this).data("id") })
                    .done((conversation) => {
                        renderConversation(conversation);
                        modal.show();
                        loadTickets();
                    })
                    .fail((xhr) => window.showAppToast(window.adminCore.errorMessage(xhr, text.loadError), "error"));
            });

        $("#messageModalTake").off("click.adminSupport").on("click.adminSupport", () => {
            if (activeConversation) postConversationAction(root.dataset.takeUrl, { id: activeConversation.id, rowVersion: activeConversation.rowVersion });
        });
        $("#messageModalRelease").off("click.adminSupport").on("click.adminSupport", () => {
            if (activeConversation) postConversationAction(root.dataset.releaseUrl, { id: activeConversation.id, rowVersion: activeConversation.rowVersion });
        });
        $("#messageModalClose").off("click.adminSupport").on("click.adminSupport", () => {
            if (activeConversation) postConversationAction(root.dataset.closeUrl, { id: activeConversation.id, rowVersion: activeConversation.rowVersion });
        });

        const sendReply = () => {
            if (!activeConversation) return;
            const message = $("#messageModalReplyText").val().trim();
            if (message.length < 10 || message.length > 4000) {
                $("#messageModalReplyError").removeClass("d-none");
                $("#messageModalReplyText").addClass("is-invalid").attr("aria-invalid", "true").trigger("focus");
                return;
            }
            $("#messageModalReplyError").addClass("d-none");
            $("#messageModalReplyText").removeClass("is-invalid").removeAttr("aria-invalid");
            postConversationAction(root.dataset.replyUrl, {
                id: activeConversation.id,
                RowVersion: activeConversation.rowVersion,
                Message: message
            });
        };
        $("#messageModalSendReply").off("click.adminSupport").on("click.adminSupport", sendReply);
        $("#messageModalReplyText").off("input.adminSupport").on("input.adminSupport", function () {
            const length = $(this).val().trim().length;
            if (length >= 10 && length <= 4000) {
                $("#messageModalReplyError").addClass("d-none");
                $(this).removeClass("is-invalid").removeAttr("aria-invalid");
            }
        });

        $(document).off("app:delete-confirmed.adminSupport", ".delete-podrska-button")
            .on("app:delete-confirmed.adminSupport", ".delete-podrska-button", function () {
                const id = $(this).data("id");
                window.appApi.post(root.dataset.deleteUrl, { id }).done((response) => {
                    window.showAppToast(response.successMessage || text.deleteSuccess, "success");
                    window.adminCore.removeRenderedItem(root, id, () => renderTickets([]));
                    pager.removeItem();
                }).fail((xhr) => window.showAppToast(window.adminCore.errorMessage(xhr, text.deleteError), "error"));
            });

        $searchButton.off("click.adminSupport").on("click.adminSupport", () => loadTickets());
        $searchInput.off("keydown.adminSupport").on("keydown.adminSupport", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                loadTickets();
            }
        });

        $queueChips.off("click.adminSupportQueue")
            .on("click.adminSupportQueue", function (event) {
                event.preventDefault();
                queueFilter = this.dataset.queueFilter || "new";
                $queueChips.removeClass("active");
                $(this).addClass("active");
                loadTickets();
            });

        $(root).off("click.adminSupportSort", ".support-sort-link")
            .on("click.adminSupportSort", ".support-sort-link", (event) => {
                event.preventDefault();
                currentDirection = currentDirection === "desc" ? "asc" : "desc";
                loadTickets();
            });

        if (!$searchInput.val()) {
            $searchInput.val(pageParams.get("section") === "Podrska"
                ? pageParams.get("searchQuery") || root.dataset.initialSearch
                : root.dataset.initialSearch);
        }
        if (pageParams.get("section") === "Podrska") {
            const requestedQueue = pageParams.get("queueFilter");
            if (validQueues.includes(requestedQueue)) queueFilter = requestedQueue;
            currentDirection = pageParams.get("direction") === "asc" ? "asc" : "desc";
        }
        updateFilterControls();
        loadTickets();
    };

    $(document).on("admin:section-loaded.adminSupport", (_, sectionName) => {
        if (sectionName === "Podrska") initialize();
    });
})();
