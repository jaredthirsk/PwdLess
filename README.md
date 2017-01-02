# PwdLess
<img src="http://pwdless.biarity.me/images/PwdLessLogo.svg" width="150">

PwdLess is a free, open-source authentication server that allows you to register/login users without a password. This is achieved by sending a "magic link" containing a nonce, possibly in the form of a URL. Once the user opens the link (or manually types the nonce into your app), a JWT is generated for the user, authenticating their identity. PwdLess operates without a database (cache only) and only requires simple configuration to run. This makes it platform-agnostic so you can easily integrate it into any tech-stack.

For more information, visit the official website: http://pwdless.biarity.me/.

# Getting Started
Getting started with PwdLess is easy:

1. Download a [PwdLess release](https://github.com/PwdLess/PwdLess/releases) for your OS of choice
 > if you don't find a build for your OS, consider [building from source](#building-from-source)

2. Add [configuration](#configuration) to the included `appsettings.json` file

3. Run PwdLess & [test it](#http-endpoints) to see if it works 

# Basic process

Here's an overview of how you can use PwdLess to authenticate a user (this is very similar to OAuth2 grants):

1. Users provide their email address & are sent a nonce
A user provides their email address to your website (ie. JS client). In turn, it makes an API call to PwdLess's `/auth/sendNonce?identifier=USER_EMAIL`. This will cause PwdLess to send the email a nonce. The email server settings are easily configurable.

2. The user opens the nonce URL or enters the nonce into your app
Once your website recieves the nonce the user recieved (by letting the user enter it manually or through query strings), you will begin requesting a JWT for the user. To do this, your website makes an API call to PwdLess's `/auth/nonceToToken?nonce=SUPPLIED_NONCE`. PwdLess will then respond with a signed JWT containing the user's email address.

3. You use the JWT to authenticate the user into your APIs
Since it is not possible to change the contents of a signed JWT (given that you validate it in your APIs), you can now be certain of the user's identity & proceed by including the JWT in the authorization header of all subsequent requests made by your website.

# HTTP Endpoints
PwdLess exposes the following HTTP API:

Arguments could be sent in a `GET` query string (as shown below), or as `POST` body values.

* `GET /auth/sendNonce?identifier=[IDENTIFIER]` where `[IDENTIFIER]` is the user's email
  * creates a nonce/token pair, stores it in cache, and sends the nonce to `[IDENTIFIER]`
  * responds `200` once email has been sent
  * responds `400` on any failiure (wrong email server settings, etc.)

* `GET /auth/nonceToToken?nonce=[NONCE]` where `[NONCE]` is the nonce to exhcnage for a token 
  * searches cache for a token associated with given nonce
  * responds `200` with the JWT (plaintext) if token found
  * responds `404` with if the token wasn't found (ie. expired)
  * responds `400` on any failiure

# Configuration
The configration is in present in the root folder, in `appsettings.json`. This tells PwdLess about everything it needs to know to start working.

A description of each configuration item:
```
  "PwdLess": {
    "Nonce": {
      "Expiry": `int: the number of minutes to pass before a nonce expires`,
      "Length": `int: the maximum length of a nonce (cutoff at 36)`
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
      "Body": `string: the body of sent emails, you add here a string "{{nonce}}" that will be replaced by the nonce once the email is sent, see wiki entry on noncess in emails`,
      "BodyType":  `string: type of message body (ie. "plain" for plaintext and "html" for HTML)`
    }
  }
```
This configuration could also be provided in the form of environment variables, where nesting is acheived by using colons (ie. "EmailContents:Subject").

To change the url/port at which the server runs (default of http://localhost:5000), supply a command line argument of `--url` (ie. `--url http://localhost:9538`)

## Rate limiting
PwdLess uses the [AspNetCoreRateLimit](https://www.nuget.org/packages/AspNetCoreRateLimit/) package for IP-based rate limiting. Rate limiting is important to prevent users from spamming emails. Rate limiting is also configurable from `appsettings.json`: refer to the [official AspNetCoreRateLimit documentation](https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/IpRateLimitMiddleware#setup) on how to do that. 

# Misc

* By default, an in-memory distributed ASP.NET Core cache used. This could easily be replaced by another one such as Redis by changing the injected caching service in the ASP.NET Core IoC container & building from source.
* For more information on the included templaing of nonces: https://github.com/PwdLess/PwdLess/wiki/Templating-&-nonces-in-emails
* For the JSON Schema of the configuration file: https://github.com/PwdLess/PwdLess/wiki/Configuration-JSON-Schema

# Building from source

This project is built on top of ASP.NET Core, which supports a variety of operating systems. Follow this guide for more information: https://docs.microsoft.com/en-us/dotnet/articles/core/deploying/.

# Design goals
PwdLess is designed to maximise ease of use and conveniece for both the developers and the users (even at the cost of not having more advanced features). With this in mind this, here are some of the rough aspects of PwdLess:

* Stateless - no database: PwdLess should preferably operate only with caches; this means PwdLess will not handle generating & storing refresh tokens (such functionality should be manually implemented if needed, or just use long-lived access tokens).
* Platform-agnostic: PwdLess should not care about the rest of your tech stack, should only be an independent server
* It should not be necessary to edit PwdLess source code: all necessary configuration should be present outside the code (ie. in `appsettings.json`)
* Advanced customization, however, should not be added to configuration: advanced configuration to customise non-PwdLess aspects such as JOSE-JWT & MailKit shoukd preferably not be customisable through configuration, instead, editing source code would be the preferred way

# License, Contributions, & Support

This project is licensed under the permissive open source [MIT license](https://opensource.org/licenses/MIT). Feel free to contribute to this project in any way, any contributions are highly appreciated.










