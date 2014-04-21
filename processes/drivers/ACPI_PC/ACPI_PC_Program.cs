/* Copyright (C) 2011 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace ACPI_PC
{
    class Program
    {
        static void Main(string[] args)
        {
            AcpiTables tables = new AcpiTables();
            aml_interpreter.AML aml;

            if (tables.Fadt == null)
                throw new Exception("FADT not found");

            unsafe 
            {
                // Parse DSDT
                ulong dsdt_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(tables.Fadt.dsdt, 36, tysos.Syscalls.MemoryFunctions.CacheType.Uncacheable, false);
                ulong dsdt_length = (ulong)*(uint *)(dsdt_vaddr + 4);
                dsdt_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(tables.Fadt.dsdt, dsdt_length, tysos.Syscalls.MemoryFunctions.CacheType.Uncacheable, false);

                aml = new aml_interpreter.AML();

                aml.SetInput(dsdt_vaddr);
                aml.LoadTable();

                // Parse SSDTs
                foreach (AcpiTables.SsdtTable ssdt in tables.Ssdts)
                {
                    aml.SetInput(ssdt.ssdt_vaddr);
                    aml.LoadTable();
                }
            }

            // Iterate through our devices deciding on what we have
            bool have_ps2_keyboard = false;
            bool have_ps2_mouse = false;
            bool have_pci = false;
            bool have_pcie = false;
            bool have_vga = false;
            bool have_pic = false;
            bool have_ioapic = false;
            foreach (KeyValuePair<string, aml_interpreter.AML.Device> kvp in aml.Devices)
            {
                if (aml.Objects.ContainsKey(kvp.Value.Name.FullNameString + "\\_HID"))
                {
                    aml_interpreter.AML.AMLData data = aml.GetObject(aml.Objects[kvp.Value.Name.FullNameString + "\\_HID"], null);
                    if (data.Type == aml_interpreter.AML.AMLData.DataType.Integer)
                    {
                        if (data.Integer == ACPI_PC.PNP_DB.PS2_Keyboard_HID)
                            have_ps2_keyboard = true;
                        else if (data.Integer == ACPI_PC.PNP_DB.PS2_Mouse_HID)
                            have_ps2_mouse = true;
                        else if (data.Integer == ACPI_PC.PNP_DB.PCI_Root_HID)
                            have_pci = true;
                        else if (data.Integer == ACPI_PC.PNP_DB.PCIe_Root_HID)
                            have_pcie = true;
                        else if (data.Integer == ACPI_PC.PNP_DB.PIC_HID)
                            have_pic = true;
                        else if (data.Integer == ACPI_PC.PNP_DB.APIC_HID)
                            have_ioapic = true;
                    }
                    else
                        tysos.Syscalls.DebugFunctions.DebugWrite("ACPI_PC: Device: " + kvp.Value.Name.FullNameString + "'s HID member is of type " + data.Type.ToString() + "\n");
                }
            }

            if ((tables.Fadt != null) && ((tables.Fadt.iapc_boot_arch & 0x4) == 0))
                have_vga = true;

            if ((tables.Apic != null) && (tables.Apic.IOAPICs.Count > 0))
                have_ioapic = true;
            if ((tables.Apic != null) && tables.Apic.Has8259)
                have_pic = true;

            // Report what we have found
            tysos.Syscalls.DebugFunctions.DebugWrite("ACPI_PC: Devices found:");
            if (have_ps2_keyboard)
                tysos.Syscalls.DebugFunctions.DebugWrite(" PS2_keyboard");
            if (have_ps2_mouse)
                tysos.Syscalls.DebugFunctions.DebugWrite(" PS2_mouse");
            if (have_pci)
                tysos.Syscalls.DebugFunctions.DebugWrite(" PCI");
            if (have_pcie)
                tysos.Syscalls.DebugFunctions.DebugWrite(" PCIe");
            if (have_vga)
                tysos.Syscalls.DebugFunctions.DebugWrite(" VGA");
            if (have_ioapic)
                tysos.Syscalls.DebugFunctions.DebugWrite(" IOAPIC");
            if (have_pic)
                tysos.Syscalls.DebugFunctions.DebugWrite(" PIC");
            tysos.Syscalls.DebugFunctions.DebugWrite("\n");

            // Report success
            tysos.Syscalls.DebugFunctions.Write("ACPI setup complete, initializing devices\n");

            // Initialize APIC
            tysos.InterruptMap imap = tysos.Syscalls.InterruptFunctions.GetInterruptMap();
            if (have_ioapic)
            {
                if (have_pic)
                    IOAPIC.DisablePIC();

                foreach (AcpiTables.ApicTable.IOAPICStructure ioapic in tables.Apic.IOAPICs)
                {
                    IOAPIC cur_apic = new IOAPIC(ioapic.ioapic_id, ioapic.ioapic_paddr, (int)ioapic.global_system_interrupt_base, tables.Apic.InterruptSourceOverrides, imap);

                }
            }
            else
                throw new Exception("No IOAPIC found - this is required");

            // Initialize PS/2 keyboard
            if (have_ps2_keyboard)
                tysos.Syscalls.ProcessFunctions.ExecModule("PS2K");

            // Initialize Vga
            if (have_vga)
                tysos.Syscalls.ProcessFunctions.ExecModule("Vga");

            // Set up the /dev filesystem
            ACPI_dev devfs = new ACPI_dev(null);
            if (have_ioapic)
                devfs.Children.Add(new vfs.DirectoryFileSystemObject("IOAPIC", devfs));
            if (have_pci)
                devfs.Children.Add(new vfs.DirectoryFileSystemObject("PCI", devfs));
            if (have_pcie)
                devfs.Children.Add(new vfs.DirectoryFileSystemObject("PCIe", devfs));
            if (have_pic && !have_ioapic)
                devfs.Children.Add(new vfs.DirectoryFileSystemObject("PIC", devfs));
            if (have_ps2_keyboard)
                devfs.Children.Add(new vfs.DirectoryFileSystemObject("PS2K", devfs));
            if (have_ps2_mouse)
                devfs.Children.Add(new vfs.DirectoryFileSystemObject("PS2M", devfs));
            if (have_vga)
                devfs.Children.Add(new vfs.DirectoryFileSystemObject("VGA", devfs));

            // Wait for the vfs to start
            tysos.ProcessEvent e = new tysos.ProcessEvent();
            e.ProcessEventType = tysos.ProcessEvent.ProcessEventTypeKind.ReadyForMessages;
            e.ProcessName = "vfs";
            tysos.Syscalls.SchedulerFunctions.Block(e);
            tysos.Process p_vfs = e.Process;

            // Mount ourselves as /dev
            vfs.vfsMessageTypes.MountMessage mm = new vfs.vfsMessageTypes.MountMessage();
            mm.mount_point = "/dev";
            mm.device = devfs;
            tysos.Syscalls.IPCFunctions.SendMessage(p_vfs, new tysos.IPCMessage { Type = vfs.vfsMessageTypes.MOUNT, Message = mm });
        }
    }
}
