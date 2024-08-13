import { registerOTel } from '@vercel/otel'
import { ConsoleSpanExporter } from '@opentelemetry/sdk-trace-web'

export function register() {
  registerOTel({
    serviceName: 'demo-web-app',
    instrumentationConfig: {
      fetch: {
        propagateContextUrls: [ /localhostL:8002/, /localhost:44302/ ]
      }
    },
    traceExporter: new ConsoleSpanExporter()
  })
}