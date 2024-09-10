'use client'

interface AppConfig {
    environment: string
    pathBase: string
    traceOtlpExporterUrl: string
    tracePropagateCorsUrls: string
    version: string
}

const windowAppConfig = typeof window !== 'undefined' ? (window as any).appConfig as AppConfig : undefined

export const appConfig: AppConfig = windowAppConfig || {
        environment: '',
        pathBase: '',
        traceOtlpExporterUrl: '',
        tracePropagateCorsUrls: '',
        version: '',
    }
