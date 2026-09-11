# Features

This document summarizes the final feature set of Autosalon OneZone.

## Public Site

- Home page with a hero section, featured vehicle and quick search.
- Vehicle catalog with search and responsive vehicle cards.
- Vehicle details page with image, price, specifications, description, reviews and loan calculator.
- Electric vehicles hide displacement fields where the value does not apply.
- Contact page for support inquiries.

## Authentication and User Accounts

- User registration.
- Login with email or username.
- Frontend and backend password validation.
- Show/hide password controls.
- Forgot-password flow with secure reset links.
- Explicitly enabled Resend delivery with a safe Development logging fallback.
- Per-IP rate limiting for password-reset requests.

## User Profile

- Profile overview.
- Edit basic account data.
- Optional password change.
- Purchased items overview.
- Review creation and editing for purchased vehicles.

## Cart and Checkout

- Add vehicles to cart.
- Remove vehicles from cart.
- Empty-cart state.
- Purchase summary.
- Mock payment flow for local demo.
- Stripe-ready payment abstraction.
- Startup validation prevents real payments from being enabled in demo mode.
- Order confirmation screen after successful purchase.

## Admin Panel

- Admin dashboard with key statistics and recent activity.
- Vehicle management.
- Vehicle search, filtering and sorting.
- User and role management.
- Review management.
- Support inquiry management.
- Inquiry status updates.
- Global delete confirmation modal instead of browser `confirm()` dialogs.

## Localization

- English as the default language.
- Bosnian as an additional language.
- Language switcher in the navbar.
- Localized frontend text, validation messages and key backend messages.
- Localized password reset email template.

## Docker Demo

- SQL Server and web app through Docker Compose.
- Automatic EF Core migrations on startup.
- Demo data seeding.
- Demo accounts configured through `.env` or public fallback values.
- Optional guarded periodic reset of mutable demo data.
- Visible demo/payment notice with a reset countdown when cleanup is enabled.

## Testing

- Unit tests for validation and business rules.
- Integration tests for backend flows.
- Tests for admin behavior, authentication, authorization and CRUD scenarios.
