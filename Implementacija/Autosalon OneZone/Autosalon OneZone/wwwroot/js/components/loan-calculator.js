(() => {
    const calculateMonthlyPayment = (amount, period, annualInterestRate) => {
        if (!Number.isFinite(amount) || amount <= 0 ||
            !Number.isInteger(period) || period <= 0 ||
            !Number.isFinite(annualInterestRate) || annualInterestRate < 0) {
            return null;
        }

        if (annualInterestRate === 0) {
            return amount / period;
        }

        const monthlyRate = annualInterestRate / 100 / 12;
        const factor = Math.pow(1 + monthlyRate, period);
        return amount * monthlyRate * factor / (factor - 1);
    };

    const updateCalculator = (calculator) => {
        const amount = Number.parseFloat(calculator.querySelector("[data-loan-amount]")?.value);
        const period = Number.parseInt(calculator.querySelector("[data-loan-period]")?.value, 10);
        const interestRate = Number.parseFloat(calculator.querySelector("[data-loan-interest]")?.value);
        const result = calculator.querySelector("[data-loan-result]");
        const monthlyPayment = calculateMonthlyPayment(amount, period, interestRate);

        if (result) {
            result.textContent = monthlyPayment === null ? "€0" : `€${monthlyPayment.toFixed(2)}`;
        }
    };

    const initializeCalculator = (calculator) => {
        if (calculator.dataset.loanCalculatorInitialized === "true") {
            return;
        }

        calculator.dataset.loanCalculatorInitialized = "true";
        calculator.querySelector("[data-loan-calculate]")?.addEventListener("click", () => updateCalculator(calculator));
        calculator.querySelectorAll("[data-loan-amount], [data-loan-period], [data-loan-interest]").forEach((field) => {
            field.addEventListener("input", () => updateCalculator(calculator));
            field.addEventListener("change", () => updateCalculator(calculator));
        });
        updateCalculator(calculator);
    };

    window.loanCalculator = {
        calculateMonthlyPayment,
        initialize(root = document) {
            root.querySelectorAll("[data-loan-calculator]").forEach(initializeCalculator);
        }
    };

    document.addEventListener("DOMContentLoaded", () => window.loanCalculator.initialize());
})();
