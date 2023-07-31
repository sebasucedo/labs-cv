# Personal CV Generator with OpenAI API and AWS Lambda

Welcome to the repository for our Personal CV Generator. This application simplifies and streamlines the process of creating a curriculum vitae (CV). It leverages the power of OpenAI's GPT-3.5-turbo to provide intelligent suggestions and improve the writing process. The project utilizes a combination of AWS Lambda functions, some of which are written in C#, providing scalability and eliminating the need for server management.

## Table of Contents

1. [Overview](#overview)
2. [Live URL](#live-url)
3. [Features](#features)
4. [Getting Started](#getting-started)
5. [How to Use](#how-to-use)
6. [Contribution](#contribution)
7. [Licence](#licence)

## Overview

The Personal CV Generator project was born out of a need to automate and simplify the process of creating a CV. With the integration of OpenAI's advanced language model, GPT-3.5-turbo, and the efficient scalability of AWS Lambda functions, we aim to make this process effortless and intuitive.

## Live URL

[Visit the live application here](https://labs.ucedo.io/cv)

## Features

- **Automated CV Generation**: Generate a CV tailored to your career and skills by answering a series of simple questions.
- **OpenAI Integration**: Uses OpenAI's GPT-3.5-turbo to provide intelligent suggestions and improve the writing process.
- **AWS Lambda Functions**: The backend consists of AWS Lambda functions, some of which are written in C#, ensuring scalability, cost-effectiveness, and robust performance.

## Getting Started

Follow these instructions to get the project up and running on your local machine for development and testing purposes.

### Prerequisites

- AWS Account
- .NET Core SDK
- Node.js
- Serverless Framework
- OpenAI API Key

### Installation

1. Clone the repository:
    ```
    git clone https://github.com/sebasucedo/labs-cv.git
    ```

2. Install the dependencies:
    ```
    npm install
    ```

3. Set up your AWS credentials and OpenAI API key as environment variables.

4. Build the C# Lambda functions:
    ```
    dotnet publish -c Release
    ```

5. Deploy the application:
    ```
    sls deploy
    ```

## How to Use

1. Navigate to the deployed AWS API Gateway endpoint.
2. Follow the prompts to input your career information.
3. Upon completion, a CV will be generated and can be downloaded in your preferred format.

## Contribution

Contributions, issues, and feature requests are welcome! For major changes, please open an issue first to discuss what you would like to change. See the [issues](https://github.com/sebasucedo/labs-cv/issues) page for ideas on where to start.

## Licence

This project is licensed under the terms of the MIT license.
