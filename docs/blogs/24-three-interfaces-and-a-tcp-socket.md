# Three Interfaces and a TCP Socket

You have a small sensor board on your balcony. An ESP32 microcontroller with a temperature sensor, a humidity sensor, and a light sensor. It cost twelve euros. It connects to an Ikon server over Wi-Fi, and now you check the weather on your balcony from your phone — not just the raw numbers, but an AI-generated forecast: "Sunny this afternoon, but humidity is climbing. Rain likely by evening. Good day to bring the laundry in before 5 PM."

The sensor board does not generate forecasts. It reads three numbers and sends them to the server. The server runs AI models that interpret the readings, correlate them with patterns, and produce a human-readable forecast. Your phone shows the result on a web page served by the same application.

This is the same architecture as the [robot in the previous post](22-native-clients-and-embedded-devices.md) — device, server, browser, one application — but the device is even simpler. No camera. No motors. Just three sensors and a Wi-Fi chip.

## Connecting

The C++ SDK that connects the sensor to the server is a set of header files you copy into your project. No installer, no package manager. It compiles on anything with a C++17 compiler.

The SDK needs three things from you: a way to log messages, a way to make one HTTP request during startup, and a way to maintain a TCP connection. You provide these as three small adapter classes. On a Raspberry Pi, you use standard Linux networking. On an ESP32, you use its built-in Wi-Fi library. On something else entirely, you use whatever that platform has. The SDK does not care — it just calls your adapters.

Here is the sensor board connecting:

```cpp
#include "ikon_sdk.h"

auto log = std::make_shared<SerialLogger>();
auto http = std::make_shared<ESP32HttpClient>();
auto tcp = std::make_shared<ESP32TcpClient>();

ikon::IkonClientConfig config;
ikon::ApiKeyConfig apiKeyConfig;
apiKeyConfig.apiKey = "ikon-xxxxx";
apiKeyConfig.spaceId = "my-balcony";
apiKeyConfig.externalUserId = "weather-sensor";
apiKeyConfig.userType = ikon::UserType::Machine;
config.apiKey = apiKeyConfig;
config.description = "Balcony Weather Station";

ikon::IkonClient client(config, log, http, tcp);

client.Ready = [&]()
{
    log->Info("Connected");
    client.SignalReady();
};

client.Connect();
```

After this runs, the sensor is connected. The server can ask it for readings. Your phone browser can see the results. The credential on the device — `ikon-xxxxx` — is an Ikon session key. Not an OpenAI key, not a cloud provider secret. Just a revocable session key. Everything sensitive lives on the server.

## The three adapters

The SDK defines three interfaces. Each one is small — a few methods that describe what the SDK needs, without dictating how you provide it.

The TCP interface is the most important one. It is also the simplest:

```cpp
class INetworkInterface
{
public:
    virtual bool Connect(const std::string& host, int port) = 0;
    virtual void Disconnect() = 0;
    virtual void Write(const uint8_t* data, size_t size) = 0;
    virtual size_t Read(uint8_t* buffer, size_t maxSize) = 0;
    virtual bool IsConnected() const = 0;
    virtual void SetConnectionClosedCallback(std::function<void()> callback) = 0;
};
```

Connect, disconnect, read bytes, write bytes. You implement this using whatever your device has — POSIX sockets on Linux, the Wi-Fi library on an ESP32, Winsock on Windows. The SDK ships example implementations for common platforms.

The HTTP interface is one method: send a request, get a response. It is only used once during startup. The logging interface is seven methods: trace through critical. Where the messages go is up to you — terminal, serial port, radio uplink, nowhere.

That is the entire integration surface. Three adapters. The SDK handles everything else: authentication, the binary protocol, session management, reconnection. Every device that implements these three adapters speaks the same protocol — the same one used by game engines, web browsers, and mobile apps.

## What you get

Once connected, the sensor board is a full participant in the Ikon session. The server can call its functions to read temperature, humidity, and light level. The AI on the server can interpret those readings and generate forecasts. Your phone browser shows the results on a live dashboard — all from the same application.

If the Wi-Fi drops during a storm, the SDK reconnects automatically with increasing delays. When the signal comes back, the session resumes.

A twelve-euro sensor board, a handful of C++ headers, three small adapter classes, and you have a device that talks to AI in the cloud and serves a live dashboard to any browser. The device reads numbers. The server thinks. The browser shows the result.

Next: [The Teddy Bear Has No Brain](25-the-teddy-bear-has-no-brain.md) — a talking toy that shows the most surprising part of this architecture.
