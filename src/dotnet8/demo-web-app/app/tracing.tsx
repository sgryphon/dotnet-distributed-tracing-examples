import { Resource } from '@opentelemetry/resources';
import { SEMRESATTRS_SERVICE_NAME } from '@opentelemetry/semantic-conventions'
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
// import { ConsoleSpanExporter } from '@opentelemetry/sdk-trace-web';
// import { SimpleSpanProcessor } from '@opentelemetry/sdk-trace-base';

const provider = new WebTracerProvider({
    resource:  new Resource({
      [SEMRESATTRS_SERVICE_NAME ]: 'demo-web-app'
    })
  });
//provider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()));
provider.register({
  //contextManager: new ZoneContextManager()
});  

const fetchInstrumentation = new FetchInstrumentation({
  propagateTraceHeaderCorsUrls: [/localhost:8002/i]
});
registerInstrumentations({
  instrumentations: [
    fetchInstrumentation,
  ],
  tracerProvider: provider
});

export async function traceSpan(name: string, fn: () => Promise<void>) {
  const tracer = provider.getTracer("client-tracer")
  await tracer.startActiveSpan(name, async (span) => {
    try {
      await fn()
    }
    finally {
      span.end()
    }
  })
}

export function TraceProvider ({ children }) {
  return (
    <>
      {children}
    </>
  );  
}
