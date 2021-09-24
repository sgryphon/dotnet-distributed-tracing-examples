import React, { Component } from 'react';

export class FetchData extends Component {
  static displayName = FetchData.name;

  constructor(props) {
    super(props);
    this.state = { forecasts: [], loading: true, value: '' };
  }

  componentDidMount() {
    this.populateWeatherData();
  }

  handleChangeValue = (e) => {
    this.setState({ value: e.target.value });
  }

  static renderForecastsTable(forecasts) {
    return (
      <table className='table table-striped' aria-labelledby="tabelLabel">
        <thead>
          <tr>
            <th>Date</th>
            <th>Temp. (C)</th>
            <th>Temp. (F)</th>
            <th>Summary</th>
          </tr>
        </thead>
        <tbody>
          {forecasts.map(forecast =>
            <tr key={forecast.date}>
              <td>{forecast.date}</td>
              <td>{forecast.temperatureC}</td>
              <td>{forecast.temperatureF}</td>
              <td>{forecast.summary}</td>
            </tr>
          )}
        </tbody>
      </table>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
      : FetchData.renderForecastsTable(this.state.forecasts);

    return (
      <div>
        <h1 id="tabelLabel" >Weather forecast</h1>
        <p>This component demonstrates fetching data from the server.</p>
        {contents}
        <p><label>Input: <input type="text" value={this.state.value} onChange={this.handleChangeValue} /></label></p>
        <p><button className="btn btn-primary" onClick={() => this.populateWeatherData()}>Refresh</button></p>
      </div>
    );
  }

  async populateWeatherData() {
    const response = await fetch('weatherforecast?' + new URLSearchParams({value: this.state.value, ts: Math.round(Date.now()/1000)}));
    const data = await response.json();
    this.setState({ forecasts: data, loading: false });
  }
}
