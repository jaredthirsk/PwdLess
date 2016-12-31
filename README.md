# PwdLess
PwdLess is a free, open-source authentication server that allows you to register/login users without a password. This is achieved by sending a "magic link" containing a time-based one-time password (in the form of a URL). Once the user opens the link (or manually types the one-time password), a JWT is generated for the user, authenticating their identity. PwdLess operates without a database (cache only) and only requires simple configuration to run.

For more information, visit the official website: http://pwdless.biarity.me/.

# Getting Started
Getting started with PwdLess is easy:

1. [Download a PwdLess release](https://github.com/PwdLess/PwdLess/releases) for your OS of choice
 > if you don't find a build for your OS, consider [building from source](#building from source)

2. [Add configuration](#configuration) to the included `appsettings.json` file

3. Run PwdLess & [test it](#http Endpoints) to see if it works 

# HTTP Endpoints
PwdLess exposes the following HTTP API:

Arguments could be sent in a `GET` query string (as shown below), or as `POST` body values.

* `GET /auth/sendtotp?identifier=[IDENTIFIER]` where `[IDENTIFIER]` is the user's email
  * creates a TOTP/token pair, stores it in cache, and sends the TOTP to `[IDENTIFIER]`
  * responds `200` once email has been sent
  * responds `400` on any failiure (wrong email server settings, etc.)

* `GET /auth/totptotoken?totp=[TOTP]` where `[TOTP]` is the TOTP to exhcnage for a token 
  * searches cache for a token associated with given totp
  * responds `200` with the JWT (plaintext) if token found
  * responds `404` with if the token wasn't found
  * responds `400` on any failiure

* `GET /auth/echo?echo=[TEXT]` where `[TEXT]` is some text to echo for testing
  * use to check if server is running properly

# Configuration
The configration is in present in the root folder, in `appsettings.json`. This tells PwdLess about everything it needs to know to start working.

A description of each configuration item:
```
  "PwdLess": {
    "Totp": {
      "Expiry": `int: the number of minutes to pass before a TOTP expires`,
      "Length": `int: the maximum length of a TOTP (cutoff at 36)`
    },
    "Jwt": {
      "SecretKey": `string: the key used to sign the JWTs to prevent it from being tampered, should only be present here and in your API for JWT validation`,
      "Issuer": `string: "iss" claim in generated JWTs`,
      "Expiry": `string: "exp" claim in generated JWTs, leave empty for 30 days`,
      "Audience": `string: "aud" claim in generated JWTs`
    },
    "EmailAuth": {
      "From": `string: email address to send emails from`,
      "Server": `string: SMTP mail server address`,
      "Port": `int: mail server port`,
      "SSL": `bool: should ssl/tls be used for email server?`,
      "Username": `string: email username`,
      "Password": `string: email password`
    },
    "EmailContents": {
      "Subject": `string: the subject of sent emails`,
      "Body": `string: the body of sent emails, you add here a string "{{totp}}" that will be replaced by the TOTP once the email is sent, see wiki entry on TOTPs in emails`,
      "BodyType":  `string: type of message body (ie. "plain" for plaintext and "html" for HTML)`
    }
  },
  ...
}
```
This configuration could also be provided in the form of environment variables, where nesting is acheived by using colons (ie. "EmailContents:Subject").

To change the url/port at which the server runs (default of http://localhost:5000), supply a command line argument of `--url` (ie. `--url http://localhost:9538`)


# Building from source

This project is built on top of ASP.NET Core, which supports a variety of operating systems. Follow this guide for more information: https://docs.microsoft.com/en-us/dotnet/articles/core/deploying/.

# License & Contributions

This project is licensed under the permissive open source [MIT license](https://opensource.org/licenses/MIT). Feel free to contribute to this project in any way, any improvements are highly appreciated.










