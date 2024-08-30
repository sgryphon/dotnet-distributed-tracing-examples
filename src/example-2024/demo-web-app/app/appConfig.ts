'use client'

interface AppConfig {
    environment: string
    pathBase: string
    tracePropagateCorsUrls: string
    version: string
}

const windowAppConfig = typeof window !== 'undefined' ? (window as any).appConfig as AppConfig : undefined

export const appConfig: AppConfig = windowAppConfig || {
        environment: '',
        pathBase: '',
        tracePropagateCorsUrls: '',
        version: '',
    }
