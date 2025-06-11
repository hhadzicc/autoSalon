| Broj kartice                 | Tip kartice       | Očekivano ponašanje / Rezultat                        |
| ---------------------------- | ----------------- | ----------------------------------------------------- |
| `4242 4242 4242 4242`        | Visa              | ✅ Uspješna transakcija                                |
| `5555 5555 5555 4444`        | MasterCard        | ✅ Uspješna transakcija                                |
| `6011 1111 1111 1117`        | Discover          | ✅ Uspješna transakcija                                |
| `3056 9300 0902 0004`        | Diners Club       | ✅ Uspješna transakcija                                |
| `4000 0000 0000 9995`        | Visa              | ❌ Odbijena - Općenito odbijanje (tok\_chargeDeclined) |
| `4000 0000 0000 0002`        | Visa              | ❌ Nedovoljno sredstava                                |
| `4000 0000 0000 0028`        | Visa              | ❌ Kartica izgubljena                                  |
| `4000 0000 0000 0036`        | Visa              | ❌ Kartica ukradena                                    |
| `4000 0000 0000 0101`        | Visa              | ❌ Kartica istekla                                     |
| `4000 0000 0000 0341`        | Visa              | ❌ Sumnja na prijevaru                                 |
| ❗ Ostali brojevi (16 cifara) | Nepoznata kartica | ❌ Odbijena - procesna greška                          |
