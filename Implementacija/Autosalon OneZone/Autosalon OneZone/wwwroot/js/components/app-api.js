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

    window.appApi = {
        request(options) {
            const settings = $.extend(true, {
                dataType: "json",
                headers: defaultHeaders
            }, options);

            settings.headers = $.extend({}, defaultHeaders, options?.headers || {});
            return $.ajax(settings);
        },

        postForm(form, options = {}) {
            const $form = $(form);
            return this.request($.extend(true, {
                url: form.action,
                type: "POST",
                data: $form.serialize()
            }, options));
        },

        getErrorMessage
    };
})();
