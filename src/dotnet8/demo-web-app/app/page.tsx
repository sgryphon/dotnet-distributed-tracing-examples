'use client'

import Image from "next/image";
import { useState } from "react";
import { TraceProvider, traceSpan } from "./tracing";
import { trace } from "@opentelemetry/api"

class ContextManager<T> {
  private contexts: Map<symbol, T>;

  constructor() {
      this.contexts = new Map();
  }

  run(context: T, fn: () => Promise<void>): Promise<void> {
      const id = Symbol();
      this.contexts.set(id, context);
      return fn().finally(() => {
          this.contexts.delete(id);
      });
  }

  getCurrentContext(): T | undefined {
      const ids = [...this.contexts.keys()];
      return this.contexts.get(ids[ids.length - 1]);
  }

  setCurrentContextValue(key: keyof T, value: any): void {
      const ids = [...this.contexts.keys()];
      const currentContext = this.contexts.get(ids[ids.length - 1]);
      if (currentContext) {
          (currentContext as any)[key] = value;
      }
  }
}

const contextManager = new ContextManager<{ user: string; value?: any; test?: string }>();

export default function Home() {
  const [fetchData, setFetchData] = useState(null)
  const [xhrData, setXhrData] = useState(null)

  const testX = async (a: number, b: number) => {
    return a + b
  }

  const fetchRequest = async () => {
    console.log("Fetching data")

    async function someAsyncFunction() {
      console.log("Current Context:", contextManager.getCurrentContext(), new Date());
      contextManager.setCurrentContextValue("value", "Some Value");
      await new Promise(resolve => setTimeout(resolve, 2000));
      console.log("Context after await:", contextManager.getCurrentContext(), new Date());
    }
    await contextManager.run({ user: "Alice" }, async () => {
      contextManager.setCurrentContextValue("test", "value");
      await someAsyncFunction();
    
      await traceSpan("fetch-request", async () => {
        console.log(
          'Before fetch:',
          trace.getActiveSpan()?.spanContext().traceId,
          trace.getActiveSpan()?.spanContext().spanId
        )
        testX(2, 3).then(async sum1 => {
          console.log(
            'After sum 1:',
            sum1,
            trace.getActiveSpan()?.spanContext().traceId,
            trace.getActiveSpan()?.spanContext().spanId
          )
          console.log("Context after sum1:", contextManager.getCurrentContext(), new Date());
          const sum2 = await testX(5, 7)
          console.log(
            'After sum 2:',
            sum2,
            trace.getActiveSpan()?.spanContext().traceId,
            trace.getActiveSpan()?.spanContext().spanId
          )
          console.log("Context after sum2:", contextManager.getCurrentContext(), new Date());
          const res = await fetch('http://localhost:8002/weatherforecast')
          console.log(
            'After fetch:',
            trace.getActiveSpan()?.spanContext().traceId,
            trace.getActiveSpan()?.spanContext().spanId
          )
          console.log("Context after fetch:", contextManager.getCurrentContext(), new Date());

          if (!res.ok) {
            throw new Error('Failed to fetch data')
          }
          const data = await res.json()
          setFetchData(data)
        })
      })
      
    });

  }

  const xhrRequest = async () => {
    console.log("XML HTTP Request")
    await traceSpan("xhr-request", async () => {
      const xhr = new XMLHttpRequest()
      console.log(
        'Before XHR:',
        trace.getActiveSpan()?.spanContext().traceId,
        trace.getActiveSpan()?.spanContext().spanId
      )
      xhr.open('GET', 'http://localhost:8002/weatherforecast', true)
      xhr.onload = () => {
        if (xhr.readyState === 4) {
          console.log(
            'XHR Success:',
            trace.getActiveSpan()?.spanContext().traceId,
            trace.getActiveSpan()?.spanContext().spanId
          )
          const data = JSON.parse(xhr.responseText)
          setXhrData(data)
        }
      }
      xhr.onerror = () => { throw new Error('XHR failed') }
      xhr.send(null)
      console.log(
        'After XHR:',
        trace.getActiveSpan()?.spanContext().traceId,
        trace.getActiveSpan()?.spanContext().spanId
      )
    })
  }

  return (
    <TraceProvider>
    <main className="flex min-h-screen flex-col items-center justify-between p-24">
      <div className="z-10 w-full max-w-5xl items-center justify-between font-mono text-sm lg:flex">
        <p className="fixed left-0 top-0 flex w-full justify-center border-b border-gray-300 bg-gradient-to-b from-zinc-200 pb-6 pt-8 backdrop-blur-2xl dark:border-neutral-800 dark:bg-zinc-800/30 dark:from-inherit lg:static lg:w-auto  lg:rounded-xl lg:border lg:bg-gray-200 lg:p-4 lg:dark:bg-zinc-800/30">
          Get started by editing&nbsp;
          <code className="font-mono font-bold">app/page.tsx</code>
        </p>
        <div className="fixed bottom-0 left-0 flex h-48 w-full items-end justify-center bg-gradient-to-t from-white via-white dark:from-black dark:via-black lg:static lg:size-auto lg:bg-none">
          <a
            className="pointer-events-none flex place-items-center gap-2 p-8 lg:pointer-events-auto lg:p-0"
            href="https://vercel.com?utm_source=create-next-app&utm_medium=appdir-template&utm_campaign=create-next-app"
            target="_blank"
            rel="noopener noreferrer"
          >
            By{" "}
            <Image
              src="/vercel.svg"
              alt="Vercel Logo"
              className="dark:invert"
              width={100}
              height={24}
              priority
            />
          </a>
        </div>
      </div>

      <div className="relative z-[-1] flex place-items-center before:absolute before:h-[300px] before:w-full before:-translate-x-1/2 before:rounded-full before:bg-gradient-radial before:from-white before:to-transparent before:blur-2xl before:content-[''] after:absolute after:-z-20 after:h-[180px] after:w-full after:translate-x-1/3 after:bg-gradient-conic after:from-sky-200 after:via-blue-200 after:blur-2xl after:content-[''] before:dark:bg-gradient-to-br before:dark:from-transparent before:dark:to-blue-700 before:dark:opacity-10 after:dark:from-sky-900 after:dark:via-[#0141ff] after:dark:opacity-40 sm:before:w-[480px] sm:after:w-[240px] before:lg:h-[360px]">
        <Image
          className="relative dark:drop-shadow-[0_0_0.3rem_#ffffff70] dark:invert"
          src="/next.svg"
          alt="Next.js Logo"
          width={180}
          height={37}
          priority
        />
      </div>

      <div className="m-5">
        <div className="m-5">
          <button className={'bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded'} onClick={fetchRequest}>Fetch Data</button>
        </div>
        <pre>{JSON.stringify(fetchData, null, 2)}</pre>
      </div>

      <div className="m-5">
        <div className="m-5">
          <button className={'bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded'} onClick={xhrRequest}>XML HTTP Request</button>
        </div>
        <pre>{JSON.stringify(xhrData, null, 2)}</pre>
      </div>

      <div className="mb-32 grid text-center lg:mb-0 lg:w-full lg:max-w-5xl lg:grid-cols-4 lg:text-left">
        <a
          href="https://nextjs.org/docs?utm_source=create-next-app&utm_medium=appdir-template&utm_campaign=create-next-app"
          className="group rounded-lg border border-transparent px-5 py-4 transition-colors hover:border-gray-300 hover:bg-gray-100 hover:dark:border-neutral-700 hover:dark:bg-neutral-800/30"
          target="_blank"
          rel="noopener noreferrer"
        >
          <h2 className="mb-3 text-2xl font-semibold">
            Docs{" "}
            <span className="inline-block transition-transform group-hover:translate-x-1 motion-reduce:transform-none">
              -&gt;
            </span>
          </h2>
          <p className="m-0 max-w-[30ch] text-sm opacity-50">
            Find in-depth information about Next.js features and API.
          </p>
        </a>

        <a
          href="https://nextjs.org/learn?utm_source=create-next-app&utm_medium=appdir-template-tw&utm_campaign=create-next-app"
          className="group rounded-lg border border-transparent px-5 py-4 transition-colors hover:border-gray-300 hover:bg-gray-100 hover:dark:border-neutral-700 hover:dark:bg-neutral-800/30"
          target="_blank"
          rel="noopener noreferrer"
        >
          <h2 className="mb-3 text-2xl font-semibold">
            Learn{" "}
            <span className="inline-block transition-transform group-hover:translate-x-1 motion-reduce:transform-none">
              -&gt;
            </span>
          </h2>
          <p className="m-0 max-w-[30ch] text-sm opacity-50">
            Learn about Next.js in an interactive course with&nbsp;quizzes!
          </p>
        </a>

        <a
          href="https://vercel.com/templates?framework=next.js&utm_source=create-next-app&utm_medium=appdir-template&utm_campaign=create-next-app"
          className="group rounded-lg border border-transparent px-5 py-4 transition-colors hover:border-gray-300 hover:bg-gray-100 hover:dark:border-neutral-700 hover:dark:bg-neutral-800/30"
          target="_blank"
          rel="noopener noreferrer"
        >
          <h2 className="mb-3 text-2xl font-semibold">
            Templates{" "}
            <span className="inline-block transition-transform group-hover:translate-x-1 motion-reduce:transform-none">
              -&gt;
            </span>
          </h2>
          <p className="m-0 max-w-[30ch] text-sm opacity-50">
            Explore starter templates for Next.js.
          </p>
        </a>

        <a
          href="https://vercel.com/new?utm_source=create-next-app&utm_medium=appdir-template&utm_campaign=create-next-app"
          className="group rounded-lg border border-transparent px-5 py-4 transition-colors hover:border-gray-300 hover:bg-gray-100 hover:dark:border-neutral-700 hover:dark:bg-neutral-800/30"
          target="_blank"
          rel="noopener noreferrer"
        >
          <h2 className="mb-3 text-2xl font-semibold">
            Deploy{" "}
            <span className="inline-block transition-transform group-hover:translate-x-1 motion-reduce:transform-none">
              -&gt;
            </span>
          </h2>
          <p className="m-0 max-w-[30ch] text-balance text-sm opacity-50">
            Instantly deploy your Next.js site to a shareable URL with Vercel.
          </p>
        </a>
      </div>
    </main>
    </TraceProvider>
  );
}
