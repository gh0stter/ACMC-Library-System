1. To make a certificate:
makecert -r -n ¡°CN=Example¡± -e 01/01/2100 -sv myselfName.pvk myselfName.cer

2. Make a developer certificate
cert2spc myselfName.cer myselfName.spc

3. Convert .pvk and .spc to .pfx
pvk2pfx -pvk my.pvk -spc my.spc -pfx my.pfx