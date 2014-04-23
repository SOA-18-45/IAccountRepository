IAccountRepository
==================

Twórcy
---
 - Materniak
 - Żelazowski
 
Opis
---
Serwis służący do zakładania konta danego klienta.

Metody:
 - createAccount()
  - przyjmuje clientId (id klienta dla ktorego ma założyć konto) oraz informacje o koncie (typ konta, oprocentowanie, czas trwania, reszta do wymyślenia)
  - zwraca id konta
 - getAccountInformation()
  - przyjmuje id konta i zwraca account information jak wyżej
