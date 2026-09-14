(() => {
    const initializeProfileForm = () => {
        const form = document.getElementById("add-profil-form");
        if (!form || form.dataset.initialized === "true") return;
        form.dataset.initialized = "true";

        const $form = $(form);
        const text = JSON.parse(form.dataset.validationTexts || "{}");
        const personNamePattern = new RegExp(form.dataset.personNamePattern);

        if (!$.validator.methods.pattern) {
            $.validator.addMethod("pattern", function (value, element, param) {
                return this.optional(element) || (param instanceof RegExp ? param.test(value) : new RegExp(param, "u").test(value));
            });
        }

        $form.validate({
            errorElement: "span",
            errorClass: "text-danger",
            rules: {
                UserName: { required: true, pattern: /^[A-Za-z0-9]+$/ },
                Ime: { required: true, pattern: personNamePattern },
                Prezime: { required: true, pattern: personNamePattern }
            },
            messages: {
                UserName: { required: text.usernameRequired, pattern: text.usernamePattern },
                Ime: { required: text.firstNameRequired, pattern: text.firstNamePattern },
                Prezime: { required: text.lastNameRequired, pattern: text.lastNamePattern }
            },
            highlight: (element) => $(element).addClass("is-invalid").removeClass("is-valid"),
            unhighlight: (element) => $(element).addClass("is-valid").removeClass("is-invalid"),
            errorPlacement: (error, element) => {
                const wrapper = element.closest(".password-wrapper");
                error.insertAfter(wrapper.length ? wrapper : element);
            }
        });

        const passwordPattern = new RegExp(form.dataset.passwordPattern);
        $("#Password").rules("add", {
            required: () => !$("#UserId").val(),
            minlength: Number(form.dataset.passwordMinLength),
            pattern: passwordPattern,
            messages: { required: text.passwordRequired, minlength: text.passwordPolicy, pattern: text.passwordPolicy }
        });
        $("#ConfirmPassword").rules("add", {
            required: () => !$("#UserId").val() || Boolean($("#Password").val()),
            equalTo: "#Password",
            messages: {
                required: !$("#UserId").val() ? text.confirmPasswordRequired : text.confirmNewPasswordRequired,
                equalTo: text.passwordsMatch
            }
        });
    };

    const initialize = () => {
        const root = document.getElementById("profili-list-container");
        if (!root || root.dataset.initialized === "true") return;
        root.dataset.initialized = "true";

        const panelRoot = document.getElementById("admin-panel-root");
        const pageParams = new URLSearchParams(window.location.search);
        const openedFromExperience = pageParams.get("section") === "Profili" && Boolean(pageParams.get("focusUserId"));
        const returnUrl = openedFromExperience ? panelRoot?.dataset.returnUrl || "" : "";
        const text = JSON.parse(root.dataset.texts || "{}");
        const { escapeHtml, escapeAttribute } = window.adminCore;
        const $tableBody = $("#profili-table-body");
        const $mobileList = $("#profili-mobile-list");
        const $searchInput = $("#profil-search-input");
        const $searchButton = $("#profil-search-button");
        const $listView = $("#profili-list-view");
        const $formContainer = $("#add-profil-form-container");
        const $formPlaceholder = $("#add-profil-form-placeholder");
        const $formTitle = $("#add-edit-profil-form-title");
        const $formDescription = $("#add-edit-profil-form-description");
        const $backToExperiences = $("#back-to-customer-experiences");
        let roleFilter = pageParams.get("section") === "Profili"
            ? pageParams.get("roleFilter") || "all"
            : "all";
        let focusedUserId = openedFromExperience ? pageParams.get("focusUserId") || "" : "";
        let listScrollPosition = 0;
        let formReturnSection = "";
        let activeListRequest = null;
        const pager = window.adminLazyList.create({
            element: root.querySelector("[data-admin-lazy-controls]"),
            loadMoreText: text.loadMore,
            onLoadMore: (offset) => loadProfiles(offset, true)
        });

        if (returnUrl) {
            $backToExperiences.prop("hidden", false);
        }

        const roleLabel = (value) => String(value || text.notAvailable).split(",").map((role) => ({
            Administrator: text.roleAdministrator,
            Prodavac: text.roleSeller,
            Kupac: text.roleBuyer
        })[role.trim()] || role.trim()).join(", ");

        const roleBadgeClass = (value) => {
            const roles = String(value || "").split(",").map((role) => role.trim());
            if (roles.includes("Administrator")) return "is-administrator";
            if (roles.includes("Prodavac")) return "is-seller";
            if (roles.includes("Kupac")) return "is-buyer";
            return "is-default";
        };

        const emailLink = (value) => {
            const email = value || text.notAvailable;
            const safeEmail = escapeHtml(email);
            if (!value) return `<span class="admin-email-link is-empty">${safeEmail}</span>`;
            const safeAttribute = escapeAttribute(email);
            return `<a class="admin-email-link" href="mailto:${safeAttribute}" title="${safeAttribute}">${safeEmail}</a>`;
        };

        const profileActions = (id, itemName) => {
            const ownAdminRow = root.dataset.currentUserIsAdmin === "true"
                && id && String(id).toLowerCase() === String(root.dataset.currentUserId).toLowerCase();
            if (ownAdminRow) {
                return `<div class="table-actions"><button type="button" class="btn btn-sm btn-outline-secondary rounded-3 admin-self-action" disabled aria-disabled="true">`
                    + `<i class="bi bi-pencil-square me-1"></i>${escapeHtml(text.edit)}</button>`
                    + `<button type="button" class="btn btn-sm btn-outline-secondary rounded-3 admin-self-action" disabled aria-disabled="true">`
                    + `<i class="bi bi-trash me-1"></i>${escapeHtml(text.delete)}</button></div>`;
            }
            return `<div class="table-actions"><button class="edit-profil-button btn btn-sm btn-outline-primary rounded-3" data-id="${escapeAttribute(id)}">`
                + `<i class="bi bi-pencil-square me-1"></i>${escapeHtml(text.edit)}</button>`
                + `<button class="delete-profil-button btn btn-sm btn-outline-danger rounded-3" data-id="${escapeAttribute(id)}" data-confirm-delete="true" `
                + `data-confirm-title="${escapeAttribute(text.deleteTitle)}" data-confirm-message="${escapeAttribute(text.deleteMessage)}" `
                + `data-confirm-item="${escapeAttribute(itemName)}" data-confirm-action="${escapeAttribute(text.deleteAction)}">`
                + `<i class="bi bi-trash me-1"></i>${escapeHtml(text.delete)}</button></div>`;
        };

        const renderProfiles = (profiles, append = false) => {
            if (!append) {
                $tableBody.empty();
                $mobileList.empty();
            }
            if (!profiles?.length) {
                if (append) return;
                const empty = `<div class="empty-state"><i class="bi bi-people"></i><p>${escapeHtml(text.empty)}</p></div>`;
                $tableBody.html(`<tr><td colspan="6">${empty}</td></tr>`);
                $mobileList.html(empty);
                return;
            }

            $.each(profiles, (_, profile) => {
                const id = profile.id || "";
                const rawUsername = profile.userName || text.notAvailable;
                const username = escapeHtml(rawUsername);
                const email = emailLink(profile.email || "");
                const firstName = escapeHtml(profile.ime || "");
                const lastName = escapeHtml(profile.prezime || "");
                const role = escapeHtml(roleLabel(profile.uloga));
                const roleClass = roleBadgeClass(profile.uloga);
                const profileButtons = profileActions(id, profile.userName || profile.email || text.customer);

                $tableBody.append(`<tr id="user-${escapeAttribute(id)}" data-admin-item-id="${escapeAttribute(id)}"><td><div class="user-cell" title="${escapeAttribute(rawUsername)}"><span class="user-avatar"><i class="bi bi-person-fill"></i></span><strong>${username}</strong></div></td>`
                    + `<td>${email}</td><td><span class="table-text-truncate" title="${escapeAttribute(profile.ime || "")}">${firstName}</span></td>`
                    + `<td><span class="table-text-truncate" title="${escapeAttribute(profile.prezime || "")}">${lastName}</span></td>`
                    + `<td><span class="admin-badge role-badge-cell ${roleClass}">${role}</span></td><td>${profileButtons}</td></tr>`);

                $mobileList.append(`<article id="user-mobile-${escapeAttribute(id)}" class="admin-review-card" data-admin-item-id="${escapeAttribute(id)}"><div class="d-flex justify-content-between gap-3 align-items-start">`
                    + `<div class="min-w-0"><span class="admin-badge role-badge-cell ${roleClass}">${role}</span><h3 title="${escapeAttribute(rawUsername)}">${username}</h3></div></div>`
                    + `<div class="review-mobile-meta"><div><span>${escapeHtml(text.email)}</span><strong>${email}</strong></div>`
                    + `<div><span>${escapeHtml(text.fullName)}</span><strong>${firstName} ${lastName}</strong></div></div>`
                    + `<div class="mobile-actions">${profileButtons}</div></article>`);
            });

            if (focusedUserId) {
                const focusedItem = root.querySelector(`[data-admin-item-id="${CSS.escape(focusedUserId)}"]`);
                window.requestAnimationFrame(() => focusedItem?.scrollIntoView({ behavior: "auto", block: "center" }));
            }
        };

        const updateUrl = () => {
            const params = new URLSearchParams();
            params.set("section", "Profili");
            if ($searchInput.val()) params.set("searchQuery", $searchInput.val());
            if (roleFilter !== "all") params.set("roleFilter", roleFilter);
            if (focusedUserId) {
                params.set("focusUserId", focusedUserId);
                if (returnUrl) params.set("returnUrl", returnUrl);
            }

            window.history.replaceState({}, "", `${window.location.pathname}?${params}`);
        };

        const loadProfiles = (offset = 0, append = false) => {
            activeListRequest?.abort();
            if (append) {
                pager.setLoading(true);
            } else {
                pager.reset();
                window.adminCore.setLoading(root, true);
                updateUrl();
            }
            const request = window.appApi.get(root.dataset.listUrl, { searchQuery: $searchInput.val(), offset, roleFilter, userIdFilter: focusedUserId })
                .done((data) => {
                    const profiles = data?.profili || [];
                    renderProfiles(profiles, append);
                    pager.update(data, profiles.length, append);
                })
                .fail((xhr, statusText) => {
                    if (statusText === "abort") return;
                    if (append) {
                        window.showAppToast(window.adminCore.errorMessage(xhr, text.loadError), "error");
                        return;
                    }
                    const message = escapeHtml(window.adminCore.errorMessage(xhr, text.loadError));
                    const error = `<div class="alert alert-danger mb-0">${message}</div>`;
                    $tableBody.html(`<tr><td colspan="6">${error}</td></tr>`);
                    $mobileList.html(error);
                }).always(() => {
                    if (activeListRequest !== request) return;
                    activeListRequest = null;
                    pager.setLoading(false);
                    if (!append) window.adminCore.setLoading(root, false);
                });
            activeListRequest = request;
        };

        const loadForm = (url, params, title, description, errorText) => {
            if ($listView.is(":visible")) {
                listScrollPosition = window.scrollY;
            }

            $formTitle.text(title);
            $formDescription.text(description);
            $listView.hide();
            $formContainer.show();
            $formPlaceholder.html(`<div class="empty-state"><i class="bi bi-hourglass-split"></i><p>${escapeHtml(text.loading)}</p></div>`);
            window.adminCore.scrollToElement($formContainer[0]);

            window.appApi.request({ url, type: "GET", data: params, dataType: "html" }).done((html) => {
                $formPlaceholder.html(html);
                window.adminCore.parseUnobtrusiveValidation($formPlaceholder.find("form"));
                initializeProfileForm();
            }).fail(() => {
                $formPlaceholder.html(`<div class="alert alert-danger mb-0">${escapeHtml(errorText)}</div>`);
            });
        };

        const showListView = (reload = false) => {
            $formContainer.hide();
            $formPlaceholder.empty();
            $listView.show();
            if (reload) loadProfiles();

            window.requestAnimationFrame(() => window.scrollTo({ top: listScrollPosition, behavior: "auto" }));
        };

        $searchButton.off("click.adminUsers").on("click.adminUsers", () => {
            focusedUserId = "";
            loadProfiles();
        });
        $searchInput.off("keydown.adminUsers").on("keydown.adminUsers", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                focusedUserId = "";
                loadProfiles();
            }
        });
        $(document).off("click.adminUsersRole", ".role-chip").on("click.adminUsersRole", ".role-chip", function () {
            roleFilter = $(this).data("role-filter") || "all";
            focusedUserId = "";
            $(".role-chip").removeClass("active");
            $(this).addClass("active");
            loadProfiles();
        });
        $backToExperiences.off("click.adminUsers").on("click.adminUsers", () => {
            if (returnUrl) window.location.assign(returnUrl);
        });
        $("#add-profil-button").off("click.adminUsers").on("click.adminUsers", function () {
            formReturnSection = $(this).data("return-section") || "";
            loadForm(root.dataset.addFormUrl, {}, text.addUser, text.addUserDescription, text.addFormLoadError);
        });
        $(document).off("click.adminUsersEdit", ".edit-profil-button").on("click.adminUsersEdit", ".edit-profil-button", function () {
            formReturnSection = "";
            loadForm(root.dataset.editFormUrl, { id: $(this).data("id") }, text.editUser, text.editUserDescription, text.editFormLoadError);
        });
        $(document).off("click.adminUsersCancel", "#cancel-add-profil, #back-to-profili-list")
            .on("click.adminUsersCancel", "#cancel-add-profil, #back-to-profili-list", () => {
                if (formReturnSection) {
                    window.loadSection(formReturnSection, {}, true);
                    return;
                }
                showListView();
            });
        $(document).off("submit.adminUsersSave", "#add-profil-form").on("submit.adminUsersSave", "#add-profil-form", function (event) {
            event.preventDefault();
            const form = this;
            const $form = $(this);
            window.adminCore.clearValidationErrors(form);
            if (!$form.valid()) return;
            window.appApi.request({ url: $form.attr("action"), type: $form.attr("method"), data: new FormData(this), processData: false, contentType: false })
                .done(() => {
                    formReturnSection = "";
                    showListView(true);
                    window.showAppToast(text.saveSuccess, "success");
                }).fail((xhr, statusText) => {
                    const messages = [];
                    if (xhr.status === 400 && xhr.responseJSON) {
                        const unmatched = window.adminCore.applyValidationErrors(form, xhr.responseJSON.errors);
                        messages.push(...unmatched);
                        if (xhr.responseJSON.identityErrors?.length) {
                            messages.push(`${text.identityErrors}\n- ${xhr.responseJSON.identityErrors.join("\n- ")}`);
                        }
                    } else {
                        messages.push(window.adminCore.errorMessage(xhr, `${xhr.status} ${statusText}`));
                    }
                    if (messages.length) {
                        window.showAppToast(messages.join("\n"), "error");
                    }
                });
        });
        $(document).off("app:delete-confirmed.adminUsers", ".delete-profil-button")
            .on("app:delete-confirmed.adminUsers", ".delete-profil-button", function () {
                const id = $(this).data("id");
                window.appApi.post(root.dataset.deleteUrl, { id })
                    .done((response) => {
                        window.showAppToast(response.successMessage || text.deleteSuccess, "success");
                        window.adminCore.removeRenderedItem(root, id, () => renderProfiles([]));
                        pager.removeItem();
                    })
                    .fail(() => window.showAppToast(text.deleteError, "error"));
            });

        if (!$searchInput.val()) {
            $searchInput.val(pageParams.get("section") === "Profili"
                ? pageParams.get("searchQuery") || root.dataset.initialSearch
                : root.dataset.initialSearch);
        }
        $(".role-chip").removeClass("active")
            .filter(`[data-role-filter="${CSS.escape(roleFilter)}"]`).addClass("active");
        loadProfiles();
    };

    $(document).on("admin:section-loaded.adminUsers", (_, sectionName) => {
        if (sectionName === "Profili") initialize();
    });
})();
