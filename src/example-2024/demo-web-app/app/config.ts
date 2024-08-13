'use client'

interface ClientConfig {
    Environment: string
    version: string
    pathBase: string
}

//const clientConfig = (window as any).config as ClientConfig | undefined
let clientConfig: ClientConfig | undefined = undefined
fetch(process.env.NEXT_PUBLIC_API_URL + 'client_config')
    .then(response => response.json())
    .then(value => { 
        clientConfig = value
    })

export const config: () => ClientConfig = () => clientConfig || {
    Environment: '',
    version: '',
    pathBase: '',
}