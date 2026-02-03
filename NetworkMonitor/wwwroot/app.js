const deviceSelect = document.getElementById("deviceSelect");
const saveButton = document.getElementById("saveButton");
const status = document.getElementById("status");

async function loadDevices() {
  const [devicesResponse, configResponse] = await Promise.all([
    fetch("/api/devices"),
    fetch("/api/config"),
  ]);

  if (!devicesResponse.ok) {
    throw new Error("Kunde inte hämta nätverkskort.");
  }

  const devices = await devicesResponse.json();
  const config = configResponse.ok ? await configResponse.json() : { deviceName: "" };

  deviceSelect.innerHTML = "";
  devices.forEach((device) => {
    const option = document.createElement("option");
    option.value = device.name;
    option.textContent = device.description
      ? `${device.description} (${device.name})`
      : device.name;
    if (device.name === config.deviceName) {
      option.selected = true;
    }
    deviceSelect.appendChild(option);
  });
}

async function saveDevice() {
  saveButton.disabled = true;
  status.hidden = true;

  try {
    const response = await fetch("/api/device", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ deviceName: deviceSelect.value }),
    });

    const data = await response.json();
    status.textContent = data.message ?? "Klart.";
    status.hidden = false;
  } catch (error) {
    status.textContent = error.message;
    status.hidden = false;
  } finally {
    saveButton.disabled = false;
  }
}

saveButton.addEventListener("click", saveDevice);

loadDevices().catch((error) => {
  status.textContent = error.message;
  status.hidden = false;
});
