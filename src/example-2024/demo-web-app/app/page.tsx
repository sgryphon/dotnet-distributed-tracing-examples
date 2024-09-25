'use client'

// import Image from "next/image";
import { useState } from "react";
import { configureOpenTelemetry, getActiveSpanContext, traceSpan } from "./tracing";
import { appConfig } from "./appConfig";

configureOpenTelemetry({
  consoleExporter: true,
  enableFetchInstrumentation: true,
  enableXhrInstrumentation: false,
  otlpExporterUrl: appConfig.traceOtlpExporterUrl,
  propagateCorsUrls: appConfig.tracePropagateCorsUrls,
  serviceName: 'demo-web-app',
  version: appConfig.version,
})  

export default function Home() {
  const [fetch3D6Result, setFetch3D6Result] = useState('')
  const [fetchWithoutSpanResult, setFetchWithoutSpanResult] = useState('')
  const [fetchND10Result, setFetchND10Result] = useState('')

  const clickFetch3D6 = async () => {
    const url = process.env.NEXT_PUBLIC_API_URL + 'api/dice/roll?dice=3D6'
    console.log('clickFetch3D6', url, getActiveSpanContext()?.traceId)
    traceSpan('click_fetch_3D6', async () => {
      return fetch(url)
        .then(response => response.json())
        .then(json => {
          console.log('clickFetch3D6 result', json, getActiveSpanContext()?.traceId)
          setFetch3D6Result(json)
        })
      })
  }
  
  const clickFetchWithoutSpan = async () => {
    const url = process.env.NEXT_PUBLIC_API_URL + 'api/dice/roll?dice=1D8'
    console.log('clickFetchND10 for N', url, getActiveSpanContext()?.traceId)
    return fetch(url)
      .then(response => response.json())
      .then(json => {
        console.log('clickFetchND10 N=', json, getActiveSpanContext()?.traceId)
        const url2 = process.env.NEXT_PUBLIC_API_URL + `api/dice/roll?dice=${json}D10`
        console.log('clickFetchND10 second query', url2, getActiveSpanContext()?.traceId)
        return fetch(url2)
          .then(response => response.json())
          .then(json => {
            console.log('clickFetchND10 result', json, getActiveSpanContext()?.traceId)
            setFetchWithoutSpanResult(json)    
          })
      })
  }

  const clickFetchND10 = async () => {
    traceSpan('click_fetch_Nd10', async () => {
      const url = process.env.NEXT_PUBLIC_API_URL + 'api/dice/roll?dice=1D8'
      console.log('clickFetchND10 for N', url, getActiveSpanContext()?.traceId)
      return fetch(url)
        .then(response => response.json())
        .then(json => {
          console.log('clickFetchND10 N=', json, getActiveSpanContext()?.traceId)
          const url2 = process.env.NEXT_PUBLIC_API_URL + `api/dice/roll?dice=${json}D10`
          console.log('clickFetchND10 second query', url2, getActiveSpanContext()?.traceId)
          return fetch(url2)
            .then(response => response.json())
            .then(json => {
              console.log('clickFetchND10 result', json, getActiveSpanContext()?.traceId)
              setFetchND10Result(json)    
            })
        })
    })
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-between p-24">
      <div className="z-10 w-full max-w-5xl text-sm flex-col">
        <h1 className="text-4xl font-extrabold dark:text-white">Dice Rolling Demo Web App</h1>
        <table className="mt-4 text-left table-auto min-w-max">
          <tbody>
            <tr>
              <td className="p-4 border-b border-blue-gray-50">
                <button className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 border border-blue-700 rounded" onClick={clickFetch3D6}>Roll 3d6</button>
              </td>
              <td className="p-4 border-b border-blue-gray-50">
                {fetch3D6Result}
              </td>
            </tr>
            <tr>
              <td className="p-4 border-b border-blue-gray-50">
                <button className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 border border-blue-700 rounded" onClick={clickFetchWithoutSpan}>Roll (1d8)d10</button>
              </td>
              <td className="p-4 border-b border-blue-gray-50">
                {fetchWithoutSpanResult}
              </td>
            </tr>
            <tr>
              <td className="p-4 border-b border-blue-gray-50">
                <button className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 border border-blue-700 rounded" onClick={clickFetchND10}>Roll (1d8)d10, with span</button>
              </td>
              <td className="p-4 border-b border-blue-gray-50">
                {fetchND10Result}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </main>
  );
}
