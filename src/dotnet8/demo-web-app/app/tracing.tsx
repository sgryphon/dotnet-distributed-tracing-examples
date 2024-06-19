import { Resource } from '@opentelemetry/resources';
import { SEMRESATTRS_SERVICE_NAME } from '@opentelemetry/semantic-conventions'
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { ConsoleSpanExporter } from '@opentelemetry/sdk-trace-web';
import { SimpleSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { trace } from "@opentelemetry/api"

const provider = new WebTracerProvider({
    resource:  new Resource({
      [SEMRESATTRS_SERVICE_NAME ]: 'demo-web-app'
    })
  });
  //provider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()));
  provider.register({
    contextManager: new ZoneContextManager()
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

//  const tracer = provider.getTracer("client-tracer")

export default function TraceProvider ({ children }) {
    return (
     <>
        {children}
     </>
    );  
  }
