# Use the official Python image from the Docker Hub
FROM python:3.12.3-slim

# Set the working directory
WORKDIR /e:/aProjects/PortfolioManager/investment-portfolio-manager/PortfolioManager/PortfolioManager.MathModule

# Copy the requirements file to the working directory
COPY requirements.txt .

# Install the dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy the rest of the application code to the working directory
COPY . .

# Specify the command to run the application
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8011"]
