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

        const loadSection = (sectionName, params = {}) => {
            const url = sectionUrls[sectionName];
            if (!url) {
                $content.html(cardMessage("warning", root.dataset.textNotFound));
                return;
            }

            $content.html(cardMessage("loading", root.dataset.textLoading, "bi-hourglass-split"));
            window.appApi.request({ url, type: "GET", data: params, dataType: "html" })
                .done((html) => renderSection(html, sectionName))
                .fail((xhr) => {
                    const message = window.adminCore.errorMessage(xhr, root.dataset.textLoadError);
                    $content.html(cardMessage("danger", message));
                });

            $links.removeClass("active");
            $links.filter(`[data-section="${sectionName}"]`).addClass("active");
        };

        $root.off("click.adminPanel", ".admin-menu-link, .dashboard-section-link")
            .on("click.adminPanel", ".admin-menu-link, .dashboard-section-link", function (event) {
                event.preventDefault();
                loadSection($(this).data("section"));
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
