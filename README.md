# PwdLess

A platform-agnostic passwordless authentication server that's a joy to use.

# What is PwdLess?

PwdLess is a free, open-source authentication server that allows you to register/login users without a password. This is achieved by sending a "magic link" containing a time-based one-time password (in the form of a URL). Once the user opens the link (or manually types the one-time password), a JWT is generated for the user, authenticating their identity. PwdLess operates without a database (cache only) and only requires simple configuration to run.

For more information, visit the official website: http://pwdless.biarity.me/.

# Getting Started
Getting started with PwdLess is easy:

1. [Download a PwdLess release](https://github.com/PwdLess/PwdLess/releases) for your OS of choice
> if you don't find a build for your OS, consider [building from source](#building)
2. [Add configuration](#configuration) to the included `appsettings.json` file
3. Run PwdLess & [test it](#http Endpoints) to see if it works 

# HTTP Endpoints
PwdLess exposes the following HTTP API:

* `GET /auth/sendtotp?identifier=[IDENTIFIER]` where `[IDENTIFIER]` is the user's email
  * creates a TOTP/token pair, stores it in cache, and sends the TOTP to `[IDENTIFIER]`
  * responds `200` once email has been sent
  * responds `400` on any failiure

* `GET /auth/totptotoken?totp=[TOTP]` where `[TOTP]` is the TOTP to exhcnage for a token 
  * searches cache for a token associated with given totp
  * responds `200` with the JWT if token found
  * responds `404` with if the token wasn't found
  * responds `400` on any failiure

* `GET /auth/echo?echo=[TEXT]` where `[TEXT]` is some text to echo for testing
  * use to check if server is running properly

# Configuration
The configration is in the form:

```
{
  "PwdLess": {
    "ClientJwtUrl": "http://YOUR_JS_CLIENT/totpcallback/?totp={{totp}}",
    "Totp": {
      "Expiry": 15,
      "Length": 10
    },
    "Jwt": {
      "SecretKey": "9e38e3-REPLACE-WITH-YOUR-SECRET-5181742b",
      "Issuer": "YOUR_ISS_NAME",
      "Expiry": "",
      "Audience": "YOUR_CLIENT_ID"
    },
    "EmailAuth": {
      "From": "EMAIL_ADDRESS_FROM",
      "Server": "EMAIL_SERVER",
      "Port": 465,
      "SSL": true,
      "Username": "EMAIL_USER",
      "Password": "EMAIL_PASSWORD"
    },
    "EmailContents": {
      "Subject": "Welcome! Continue your passwordless Auth here.",
      "Body": "Continue by opening this link: {{url}}. Alternatively, use the following code: {{totp}}",
      "BodyType":  "plain"
    }
  },
  ...
}

```

A description of each:
```
  "PwdLess": {
    "ClientJwtUrl": `string: containing "{{totp}}", this is the url sent to the user's email`,
    "Totp": {
      "Expiry": `int: the number of minutes that'll pass before a TOTP expires`,
      "Length": `int: the maximum length of a TOTP`
    },
    "Jwt": {
      "SecretKey": `string: the key used to sign the JWT & prevent it from being tampered`,
      "Issuer": `string: "iss" claim in JWT`,
      "Expiry": `string: "exp" claim in JWT, default is 30 days`,
      "Audience": `string: "aud" claim in JWT`
    },
    "EmailAuth": {
      "From": "EMAIL_ADDRESS_FROM",
      "Server": "EMAIL_SERVER",
      "Port": 465,
      "SSL": true,
      "Username": "EMAIL_USER",
      "Password": "EMAIL_PASSWORD"
    },
    "EmailContents": {
      "Subject": "Welcome! Continue your passwordless Auth here.",
      "Body": "Continue by opening this link: {{url}}. Alternatively, use the following code: {{totp}}",
      "BodyType":  "plain"
    }
  },
  ...
}
```
# Building



















<h4>Getting Started</h4>
              Getting started with PwdLess is easy!
              <p>
                <li>1. <a href="https://github.com/PwdLess/PwdLess/releases">Download a PwdLess release</a> for your OS of choice</li>
                <li>2. <a href="#config">Add your configuration</a> to the included appsettings.json file</li>
                <li>3. Run PwdLess &amp enjoy</li>
                <li>4. <a href="#api">Test it</a> to see if it's working</li>
              </p>
              
              <h4 id="api">HTTP Endpoints</h4>
               

              <h4 id="config">Configuration</h4>
