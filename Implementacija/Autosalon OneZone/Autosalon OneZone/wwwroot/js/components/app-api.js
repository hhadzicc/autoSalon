(() => {
    if (window.appApi) {
        return;
    }

    const defaultHeaders = {
        Accept: "application/json",
        "X-Requested-With": "XMLHttpRequest"
    };

    const getErrorMessage = (xhr, fallback = "Request failed.") => {
        const response = xhr?.responseJSON;
        if (!response) {
            return fallback;
        }

        if (response.message) {
            return response.message;
        }

        if (response.errors) {
            const messages = [];
            $.each(response.errors, (_, fieldErrors) => {
                if (Array.isArray(fieldErrors)) {
                    messages.push(...fieldErrors);
                } else if (fieldErrors) {
                    messages.push(String(fieldErrors));
                }
            });

            if (messages.length > 0) {
                return messages.join(" ");
            }
        }

        return fallback;
    };

    const antiforgeryHeaders = (root = document) => {
        const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value;
        return token ? { RequestVerificationToken: token } : {};
    };

    window.appApi = {
        request(options) {
            const settings = $.extend({}, {
                dataType: "json",
                headers: defaultHeaders
            }, options);

            settings.headers = $.extend({}, defaultHeaders, options?.headers || {});
            return $.ajax(settings);
        },

        postForm(form, options = {}) {
            const $form = $(form);
            return this.request($.extend({}, {
                url: form.action,
                type: "POST",
                data: $form.serialize()
            }, options));
        },

        get(url, data = {}, options = {}) {
            return this.request($.extend({}, {
                url,
                type: "GET",
                data
            }, options));
        },

        post(url, data = {}, options = {}) {
            return this.request($.extend({}, {
                url,
                type: "POST",
                data,
                headers: antiforgeryHeaders()
            }, options));
        },

        getErrorMessage,
        antiforgeryHeaders
    };
})();
