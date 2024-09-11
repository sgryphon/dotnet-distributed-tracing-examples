import { context, Context, SpanContext, trace } from '@opentelemetry/api'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { registerInstrumentations } from '@opentelemetry/instrumentation'
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch'
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request'
import { Resource } from '@opentelemetry/resources'
import {
    BatchSpanProcessor,
    ConsoleSpanExporter,
    SimpleSpanProcessor,
    WebTracerProvider,
} from '@opentelemetry/sdk-trace-web'
import {
    ATTR_SERVICE_NAME,
    ATTR_SERVICE_VERSION,
} from '@opentelemetry/semantic-conventions'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';

export const getActiveSpanContext = () => trace.getActiveSpan()?.spanContext()

// NOTE: The following represents and overloaded function in TypeScript.
// The exported different signatures are listed first, then the actual implementation is
// a single function with multi-purpose optional args. To determine the overload called
// the arg types are checked and then cast to the correct types.

// The two overloads are:
// * traceSpan with implicit parent context, using context.active(), if any, as the parent
// * traceSpan with an explicit parent context, e.g. when passed in via navigate state or some other means

export async function traceSpan<T>(name: string, fn: () => Promise<T>): Promise<T>
export async function traceSpan<T>(
    name: string,
    spanContext: SpanContext,
    fn: () => Promise<T>
): Promise<T>
export async function traceSpan<T>(
    name: string,
    arg2: (() => Promise<T>) | SpanContext,
    arg3?: () => Promise<T>
): Promise<T> {
    const tracer = trace.getTracer('client-tracer')
    let fn: () => Promise<T>
    let ctx: Context

    if (typeof arg2 === 'function') {
        // Case without spanContext
        fn = arg2 as () => Promise<T>
        ctx = context.active()
    } else {
        // Case with spanContext
        const spanContext = arg2 as SpanContext
        fn = arg3 as () => Promise<T>
        ctx = trace.setSpanContext(context.active(), spanContext)
    }
    return tracer.startActiveSpan(name, {}, ctx, async span => {
        return fn().finally(() => {
            span.end()
        })
    })
}

interface ConfigureOpenTelemetryProps {
    consoleExporter?: boolean
    enableFetchInstrumentation?: boolean
    enableXhrInstrumentation?: boolean
    otlpExporterUrl?: string
    propagateCorsUrls?: string
    serviceName: string
    version?: string
}

export const configureOpenTelemetry = ({
    consoleExporter,
    enableFetchInstrumentation = true,
    enableXhrInstrumentation = true,
    otlpExporterUrl,
    propagateCorsUrls,
    serviceName,
    version,
}: ConfigureOpenTelemetryProps) => {
    const tracerProvider = new WebTracerProvider({
        resource: new Resource({
            [ATTR_SERVICE_NAME]: serviceName,
            [ATTR_SERVICE_VERSION]: version,
        }),
    })
    if (consoleExporter) {
        tracerProvider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()))
    }
    if (otlpExporterUrl) {
        tracerProvider.addSpanProcessor(new BatchSpanProcessor(new OTLPTraceExporter({url: otlpExporterUrl})))
    }
    tracerProvider.register({
        contextManager: new ZoneContextManager(),
    })

    const propagateCorsMap = propagateCorsUrls?.split(',').map(x => new RegExp(x.trim()))

    const instrumentations = []
    if (enableFetchInstrumentation) {
        const fetchInstrumentation = new FetchInstrumentation({
            propagateTraceHeaderCorsUrls: propagateCorsMap,
        })
        instrumentations.push(fetchInstrumentation)
    }
    if (enableXhrInstrumentation) {
        const xhrInstrumentation = new XMLHttpRequestInstrumentation({
            propagateTraceHeaderCorsUrls: propagateCorsMap,
        })
        instrumentations.push(xhrInstrumentation)
    }

    registerInstrumentations({
        instrumentations: instrumentations,
        tracerProvider: tracerProvider,
    })
}
