# Dotnet Distributed Tracing Examples
Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.


## Requirements

* Dotnet 5.0
* Powershell, for running scripts

## Basic example

Front end is a little special, so lets just start with server to server calls.

**NOTE:** If you have trouble with HTTPS, or do not have certificats set up, then see the section at
the end of this file for HTTPS Developer Certificates.

### New back end service

Create a development certificate (if you are using a different shell, replace the PowerShell variable):

Create a directory for the project and a solution file

```pwsh
mkdir 1-basic
cd 1-basic
dotnet new sln
dotnet new webapi --output Demo.Service
dotnet sln add Demo.Service
```

Check it works:

```
dotnet run --project Demo.Service --urls "https://[::]:44301" --environment Development --Logging:LogLevel:Default Debug
```

Test it in a browser at `https://localhost:44301/WeatherForecast`

### New web + api server

In another terminal:

```
cd 1-basic
dotnet new react --output Demo.WebApp
dotnet sln add Demo.WebApp
```

Check it works:

```
dotnet run --project Demo.WebApp --urls "https://[::]:44302" --environment Development --Logging:LogLevel:Default Debug
```

Test it in a browser at `https://localhost:44302` 

### Add a button to trigger the weather service



### Link the two components

* Add/modify button on web to send text box value; server to call back end service (injected HttpClient)
  - mention special magic about HttpClient; use the pre-configured IoC version
* Add logging (console, with context)
* It just works





## HTTPS Developer Certificates

### Windows and macOS

See: https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio#trust-the-aspnet-core-https-development-certificate-on-windows-and-macos

The certificate is automatically installed. To trust the certificate:

```
dotnet dev-certs https --trust
```

### Ubuntu

See: https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio#ubuntu-trust-the-certificate-for-service-to-service-communication

You need to have OpenSSL installed (check with `openssl version`).

Create the HTTPS developer certificate for the current user personal certificate store (if not already initialised). 

```
dotnet dev-certs https
```

You can check the certificate exists for the current user; the file name is the SHA1 thumbprint. (If you want to clear out previous certificates use `dotnet dev-certs https --clean`, which will delete the file.)

```
ls ~/.dotnet/corefx/cryptography/x509stores/my
```

Install the certificate. You need to use the `-E` flag with `sudo` when exporting the file, so that it exports the file for the current user (otherwise it will export the file for root, which will be different).

```
sudo -E dotnet dev-certs https -ep /usr/local/share/ca-certificates/aspnet/https.crt --format PEM
sudo update-ca-certificates
```

You can check the file exists, and then use open SSL to verify it has the same SHA1 thumbprint.

```
ls /usr/local/share/ca-certificates/aspnet
openssl x509 -noout -fingerprint -sha1 -inform pem -in /usr/local/share/ca-certificates/aspnet/https.crt
```

If the thumbprints do not match, you may have install the root (sudo user) certificate. You can check it at `sudo ls -la /root/.dotnet/corefx/cryptography/x509stores/my`.


Trust in Chrome:

```
sudo apt-get install -y libnss3-tools
certutil -d sql:$HOME/.pki/nssdb -A -t "P,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
certutil -d sql:$HOME/.pki/nssdb -A -t "C,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
```

Trust in Firefox:

```
cat <<EOF | sudo tee /usr/lib/firefox/distribution/policies.json
{
    "policies": {
        "Certificates": {
            "Install": [
                "/usr/local/share/ca-certificates/aspnet/https.crt"
            ]
        }
    }
}
EOF
```
