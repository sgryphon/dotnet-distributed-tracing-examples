'use client'

import Image from "next/image";
import { config } from "./config";
import { useState } from "react";

export default function Home() {
  const [fetchD6Result, setFetchD6Result] = useState('')

  const clickFetchD6 = async () => {
    const url = process.env.NEXT_PUBLIC_API_URL + 'api/dice/roll?dice=d6'
    console.log('clickFetchD6', url)
    const response = await fetch(url)
    setFetchD6Result(await response.json())
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-between p-24">
      <div className="z-10 w-full max-w-5xl text-sm flex-col">
        <h1>Demo Web App</h1>
        <p>
          <button className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 border border-blue-700 rounded" onClick={clickFetchD6}>Fetch D6</button>
        </p>
        <p>
          Result: {fetchD6Result}
        </p>
      </div>
    </main>
  );
}
