Babbacombe.Webserver
====================

A simple, lean, extendable Webserver in a dll for incorporation into .net/mono projects.

This basic implementation is intended for use only on intranets (or over a VPN) and contains no security whatsoever. **DO NOT** connect an app using it directly to the internet.  

If you try to run an app using the webserver on Windows, but get an Access Denied error, you either need to run as Administrator or configure the port. On Vista or later you can configure it by entering something like
```
netsh http add urlacl url=http://+:80/ user=DOMAIN\User listen=yes
```
at an Administrator command prompt.

See [the project's wiki](https://github.com/trevorprinn/Babbacombe.Webserver/wiki) for more information.
