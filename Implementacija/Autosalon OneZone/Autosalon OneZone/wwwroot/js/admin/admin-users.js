(() => {
    const initializeProfileForm = () => {
        const form = document.getElementById("add-profil-form");
        if (!form || form.dataset.initialized === "true") return;
        form.dataset.initialized = "true";

        const $form = $(form);
        const text = JSON.parse(form.dataset.validationTexts || "{}");
        $("#Ime, #Prezime").off("input.adminUserForm").on("input.adminUserForm", function () {
            $(this).val($(this).val().replace(/[^\p{L}\s-]/gu, ""));
        });
        $("#UserName").off("input.adminUserForm").on("input.adminUserForm", function () {
            $(this).val($(this).val().replace(/[^A-Za-z0-9]/g, ""));
        });

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
                Ime: { required: true, pattern: /^[\p{L}\s-]+$/u },
                Prezime: { required: true, pattern: /^[\p{L}\s-]+$/u }
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

        const text = JSON.parse(root.dataset.texts || "{}");
        const { escapeHtml, escapeAttribute } = window.adminCore;
        const $tableBody = $("#profili-table-body");
        const $mobileList = $("#profili-mobile-list");
        const $searchInput = $("#profil-search-input");
        const $searchButton = $("#profil-search-button");
        const $formContainer = $("#add-profil-form-container");
        const $formPlaceholder = $("#add-profil-form-placeholder");
        const $formTitle = $("#add-edit-profil-form-title");
        let roleFilter = "all";

        const roleLabel = (value) => String(value || text.notAvailable).split(",").map((role) => ({
            Administrator: text.roleAdministrator,
            Prodavac: text.roleSeller,
            Kupac: text.roleBuyer
        })[role.trim()] || role.trim()).join(", ");

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

        const renderProfiles = (profiles) => {
            $tableBody.empty();
            $mobileList.empty();
            if (!profiles?.length) {
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
                const profileButtons = profileActions(id, profile.userName || profile.email || text.customer);

                $tableBody.append(`<tr><td><div class="user-cell" title="${escapeAttribute(rawUsername)}"><span class="user-avatar"><i class="bi bi-person-fill"></i></span><strong>${username}</strong></div></td>`
                    + `<td>${email}</td><td><span class="table-text-truncate" title="${escapeAttribute(profile.ime || "")}">${firstName}</span></td>`
                    + `<td><span class="table-text-truncate" title="${escapeAttribute(profile.prezime || "")}">${lastName}</span></td>`
                    + `<td><span class="admin-badge role-badge-cell">${role}</span></td><td>${profileButtons}</td></tr>`);

                $mobileList.append(`<article class="admin-review-card"><div class="d-flex justify-content-between gap-3 align-items-start">`
                    + `<div class="min-w-0"><span class="admin-badge">${role}</span><h3 title="${escapeAttribute(rawUsername)}">${username}</h3></div></div>`
                    + `<div class="review-mobile-meta"><div><span>${escapeHtml(text.email)}</span><strong>${email}</strong></div>`
                    + `<div><span>${escapeHtml(text.fullName)}</span><strong>${firstName} ${lastName}</strong></div></div>`
                    + `<div class="mobile-actions">${profileButtons}</div></article>`);
            });
        };

        const loadProfiles = () => {
            $tableBody.html(`<tr><td colspan="6">${escapeHtml(text.loading)}</td></tr>`);
            $mobileList.html(`<div class="empty-state"><i class="bi bi-hourglass-split"></i><p>${escapeHtml(text.loading)}</p></div>`);
            window.appApi.get(root.dataset.listUrl, { searchQuery: $searchInput.val(), page: 1, roleFilter })
                .done((data) => renderProfiles(data?.profili || []))
                .fail((xhr) => {
                    const message = escapeHtml(window.adminCore.errorMessage(xhr, text.loadError));
                    const error = `<div class="alert alert-danger mb-0">${escapeHtml(text.loadError)} ${message}</div>`;
                    $tableBody.html(`<tr><td colspan="6">${error}</td></tr>`);
                    $mobileList.html(error);
                });
        };

        const loadForm = (url, params, title, errorText) => {
            $formTitle.text(title);
            window.appApi.request({ url, type: "GET", data: params, dataType: "html" }).done((html) => {
                $formPlaceholder.html(html);
                $formContainer.show();
                window.adminCore.parseUnobtrusiveValidation($formPlaceholder.find("form"));
                initializeProfileForm();
            }).fail(() => {
                $formPlaceholder.html(`<div class="alert alert-danger mb-0">${escapeHtml(errorText)}</div>`);
                $formContainer.show();
            });
        };

        $searchButton.off("click.adminUsers").on("click.adminUsers", loadProfiles);
        $searchInput.off("keydown.adminUsers").on("keydown.adminUsers", (event) => {
            if (event.key === "Enter") { event.preventDefault(); loadProfiles(); }
        });
        $(document).off("click.adminUsersRole", ".role-chip").on("click.adminUsersRole", ".role-chip", function () {
            roleFilter = $(this).data("role-filter") || "all";
            $(".role-chip").removeClass("active");
            $(this).addClass("active");
            loadProfiles();
        });
        $("#add-profil-button").off("click.adminUsers").on("click.adminUsers", () => loadForm(root.dataset.addFormUrl, {}, text.addUser, text.addFormLoadError));
        $(document).off("click.adminUsersEdit", ".edit-profil-button").on("click.adminUsersEdit", ".edit-profil-button", function () {
            loadForm(root.dataset.editFormUrl, { id: $(this).data("id") }, text.editUser, text.editFormLoadError);
        });
        $(document).off("click.adminUsersCancel", "#cancel-add-profil").on("click.adminUsersCancel", "#cancel-add-profil", () => {
            $formContainer.hide();
            $formPlaceholder.empty();
        });
        $(document).off("submit.adminUsersSave", "#add-profil-form").on("submit.adminUsersSave", "#add-profil-form", function (event) {
            event.preventDefault();
            const $form = $(this);
            if (!$form.valid()) return;
            window.appApi.request({ url: $form.attr("action"), type: $form.attr("method"), data: new FormData(this), processData: false, contentType: false })
                .done(() => {
                    $formContainer.hide();
                    $formPlaceholder.empty();
                    loadProfiles();
                    window.showAppToast(text.saveSuccess, "success");
                }).fail((xhr, statusText) => {
                    let message = text.saveError;
                    if (xhr.status === 400 && xhr.responseJSON) {
                        $.each(xhr.responseJSON.errors || {}, (key, messages) => {
                            const $span = $(`span[data-valmsg-for="${key}"]`);
                            if ($span.length) $span.text(messages.join(", ")).show();
                            else message += `\n${key}: ${messages.join(", ")}`;
                        });
                        if (xhr.responseJSON.identityErrors?.length) {
                            message += `\n\n${text.identityErrors}\n- ${xhr.responseJSON.identityErrors.join("\n- ")}`;
                        }
                    } else {
                        message += `\n${window.adminCore.errorMessage(xhr, `${xhr.status} ${statusText}`)}`;
                    }
                    window.showAppToast(message, "error");
                });
        });
        $(document).off("app:delete-confirmed.adminUsers", ".delete-profil-button")
            .on("app:delete-confirmed.adminUsers", ".delete-profil-button", function () {
                window.appApi.post(root.dataset.deleteUrl, { id: $(this).data("id") })
                    .done((response) => { window.showAppToast(response.successMessage || text.deleteSuccess, "success"); loadProfiles(); })
                    .fail(() => window.showAppToast(text.deleteError, "error"));
            });

        if (!$searchInput.val() && root.dataset.initialSearch) $searchInput.val(root.dataset.initialSearch);
        loadProfiles();
    };

    $(document).on("admin:section-loaded.adminUsers", (_, sectionName) => {
        if (sectionName === "Profili") initialize();
    });
})();
