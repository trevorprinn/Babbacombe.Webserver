Babbacombe.Webserver
====================

A simple, lean, extendable Webserver in a dll for incorporation into .net/mono projects.

If you try to run an app using the webserver, but get an Access Denied error, you either need to run as Administrator or configure the port. On Vista or later you can configure it by entering something like
netsh http add urlacl url=http://+:80/ user=DOMAIN\User listen=yes
at an Administrator command prompt.