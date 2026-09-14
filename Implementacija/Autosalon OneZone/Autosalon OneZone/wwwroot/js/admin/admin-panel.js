(() => {
    const initialize = () => {
        const root = document.getElementById("admin-panel-root");
        if (!root || !window.jQuery || !window.adminCore) {
            return;
        }

        const $root = $(root);
        const $content = $("#admin-section-content");
        const $links = $root.find(".admin-menu-link");
        const sectionUrls = {
            Dashboard: root.dataset.urlDashboard,
            Vozila: root.dataset.urlVozila,
            Profili: root.dataset.urlProfili,
            Recenzije: root.dataset.urlRecenzije,
            Podrska: root.dataset.urlPodrska
        };
        let activeSectionRequest = null;

        const cardMessage = (kind, message, icon = "") => {
            const safeMessage = window.adminCore.escapeHtml(message);
            if (kind === "loading") {
                return `<div class="admin-card"><div class="empty-state"><i class="bi ${icon}"></i><p>${safeMessage}</p></div></div>`;
            }
            return `<div class="admin-card"><div class="alert alert-${kind} mb-0">${safeMessage}</div></div>`;
        };

        const renderSection = (html, sectionName) => {
            $content.html(html);
            $content.trigger("admin:section-loaded", [sectionName]);
        };

        const updateSectionUrl = (sectionName) => {
            const params = new URLSearchParams();
            if (sectionName !== "Dashboard") {
                params.set("section", sectionName);
            }

            const query = params.toString();
            window.history.replaceState({}, "", `${window.location.pathname}${query ? `?${query}` : ""}`);
        };

        const loadSection = (sectionName, params = {}, updateUrl = false) => {
            const url = sectionUrls[sectionName];
            if (!url) {
                $content.html(cardMessage("warning", root.dataset.textNotFound));
                return;
            }

            if (updateUrl) {
                updateSectionUrl(sectionName);
            }

            activeSectionRequest?.abort();
            $content.html(cardMessage("loading", root.dataset.textLoading, "bi-hourglass-split"));
            const request = window.appApi.request({ url, type: "GET", data: params, dataType: "html" })
                .done((html) => renderSection(html, sectionName))
                .fail((xhr, statusText) => {
                    if (statusText === "abort") return;
                    const message = window.adminCore.errorMessage(xhr, root.dataset.textLoadError);
                    $content.html(cardMessage("danger", message));
                })
                .always(() => {
                    if (activeSectionRequest === request) {
                        activeSectionRequest = null;
                    }
                });
            activeSectionRequest = request;

            $links.removeClass("active");
            $links.filter(`[data-section="${sectionName}"]`).addClass("active");
            return request;
        };

        $root.off("click.adminPanel", ".admin-menu-link, .dashboard-section-link")
            .on("click.adminPanel", ".admin-menu-link, .dashboard-section-link", function (event) {
                event.preventDefault();
                loadSection($(this).data("section"), {}, true);
            });

        $root.off("click.adminPanelCreate", ".dashboard-create-link")
            .on("click.adminPanelCreate", ".dashboard-create-link", function (event) {
                event.preventDefault();
                const $button = $(this);
                const request = loadSection($button.data("section"), {}, true);
                request?.done(() => {
                    const $trigger = $content.find($button.data("form-trigger")).first();
                    if (!$trigger.length) return;
                    $trigger.data("return-section", $button.data("return-section") || "Dashboard");
                    $trigger.trigger("click");
                });
            });

        let defaultSection = root.dataset.defaultSection || "Dashboard";
        if (defaultSection === "Profili" && root.dataset.isAdmin !== "true") {
            defaultSection = "Dashboard";
        }

        window.loadSection = loadSection;
        loadSection(defaultSection);
    };

    $(initialize);
})();
