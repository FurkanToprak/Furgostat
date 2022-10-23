# Furgostat
An All-In-One Controller for the Chemostat, Turbidostat, and the Morbidostat

This is the world's only actively maintained open source code for the Chemostat, Turbidostat, and Morbidostat.

## Changes you may have to make:
In `Core.cs` you will have to configure the following arrays such that they reflect your Relay configuration.

Each array represents a collection of Relay connections, such that each connection is {<Relay Box Number, Connection Number on that Relay Box>}

```
Suction = new int[] { 1, 22 };
            MediaRelayIDs = new int[,] { { 0, 1 }, { 0, 4 }, { 0, 7 }, { 0, 10 }, { 0, 13 }, { 0, 16 }, { 0, 19 }, { 0, 22 }, { 1, 1 }, { 1, 4 }, { 1, 7 }, { 1, 10 }, { 1, 13 }, { 1, 16 }, { 1, 19 } };
            DrugARelayIDs = new int[,] { { 0, 2 }, { 0, 5 }, { 0, 8 }, { 0, 11 }, { 0, 14 }, { 0, 17 }, { 0, 20 }, { 0, 23 }, { 1, 2 }, { 1, 5 }, { 1, 8 }, { 1, 11 }, { 1, 14 }, { 1, 17 }, { 1, 20 } };
            DrugBRelayIDs = new int[,] { { 0, 3 }, { 0, 6 }, { 0, 9 }, { 0, 12 }, { 0, 15 }, { 0, 18 }, { 0, 21 }, { 0, 24 }, { 1, 3 }, { 1, 6 }, { 1, 9 }, { 1, 12 }, { 1, 15 }, { 1, 18 }, { 1, 21 } };
            LEDPDSwitch = new int[] { 1, 24 }; //currently active but circuit is not modified yet
```

A more talented and ambitious Furkan would have turned this into a config file that's better documented, but who strays from sloth?
