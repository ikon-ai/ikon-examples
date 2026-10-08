# Three Interfaces and a TCP Socket

*Published 2026-03-31*

You have a small sensor board on your balcony. It is an ESP32 microcontroller with a temperature sensor, a humidity sensor and a light sensor, and it cost twelve euros. It connects to an Ikon server over Wi-Fi, and you check the weather on your balcony from your phone. Besides the raw numbers, the phone shows an AI-generated forecast: "Sunny this afternoon, but humidity is climbing. Rain likely by evening. Good day to bring the laundry in before 5 PM."

The sensor board does not generate forecasts. It reads three numbers and sends them to the server. The server runs AI models that interpret the readings, compare them with patterns and write a forecast in plain language. Your phone shows the forecast on a web page served by the same application.

This is the same setup as the [robot in the previous post](22-native-clients-and-embedded-devices.md): a device, a server and a browser, all part of one application. The device here is simpler. It has no camera and no motors, only three sensors and a Wi-Fi chip.

## Connecting

The C++ SDK that connects the sensor to the server is a set of header files you copy into your project. You don't need an installer or a package manager. It compiles on anything with a C++17 compiler.

The SDK needs three things from you: a way to log messages, a way to make one HTTP request during startup, and a way to keep a TCP connection open. You provide these as three small adapter classes. On a Raspberry Pi, you use standard Linux networking. On an ESP32, you use its built-in Wi-Fi library. On any other device, you use whatever that platform has. The SDK only calls your adapters, so it does not depend on the platform.

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

After this code runs, the sensor is connected. The server can ask it for readings, and the browser on your phone can show the results. The only credential on the device, `ikon-xxxxx`, is an Ikon session key that you can revoke. It is not an OpenAI key or any other cloud provider secret. Those secrets stay on the server.

## The three adapters

The SDK defines three interfaces. Each has only a few methods, which say what the SDK needs and leave the implementation to you.

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

It connects, disconnects, reads bytes and writes bytes. You implement it with whatever your device has, such as POSIX sockets on Linux, the Wi-Fi library on an ESP32, or Winsock on Windows. The SDK ships example implementations for common platforms.

The HTTP interface has one method, which sends a request and returns the response. It is used only once, during startup. The logging interface has seven methods: one to initialize it and one for each of the six levels from trace to critical. You decide where the messages go: a terminal, a serial port, a radio uplink, or nowhere.

These three adapters are all you write to connect a device. The SDK handles everything else: authentication, the binary protocol, session management and reconnection. Every device that implements the three adapters uses the same protocol as game engines, web browsers and mobile apps.

## What you get

Once connected, the sensor board takes part in the Ikon session like any other client. The server can call its functions to read temperature, humidity and light level. The AI on the server can interpret those readings and generate forecasts. Your phone's browser shows the results on a live dashboard, and the device, the server and the dashboard all belong to the same application.

If the Wi-Fi drops during a storm, the SDK reconnects automatically, waiting longer between each attempt. When the signal comes back, the session resumes.

With a twelve-euro sensor board, a few C++ header files and three small adapter classes, you have a device that sends its readings to AI models in the cloud, and a live dashboard that any browser can open. The device reads numbers, the server runs the AI models, and the browser shows the result.

Next: [The Teddy Bear Has No Brain](25-the-teddy-bear-has-no-brain.md), a talking toy with no AI on the device, where the server decides everything it says.
