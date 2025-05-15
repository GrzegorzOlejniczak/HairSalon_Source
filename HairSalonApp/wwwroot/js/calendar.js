const CALENDAR_CONFIG = {
    monthNames: [
        "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
        "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
    ],
    selectors: {
        days: ".days",
        hours: ".hours li:not(.taken)",
        serviceOptions: ".service-options",
        monthLabel: "#monthText",
        prevButton: ".prev",
        nextButton: ".next",
        submitButton: "button",
        hairdresserId: "input[name='HairdresserId']",
        appointmentDate: "input[name='AppointmentDate']",
        serviceSelect: "select[name='ServiceId']"
    }
};

// Klasa zarządzająca stanem kalendarza
class CalendarState {
    constructor() {
        const now = new Date();
        this.currentYear = now.getFullYear();
        this.currentMonth = now.getMonth();
        this.currentDay = now.getDate();
        this.currentHour = now.getHours();
        this.selectedYear = this.currentYear;
        this.selectedMonth = this.currentMonth;
    }


    getDateString() {
        return `${this.selectedYear}-${String(this.selectedMonth + 1).padStart(2, '0')}-${String(this.currentDay).padStart(2, '0')}T${String(this.currentHour).padStart(2, '0')}:00`;
    }

    canGoToPreviousMonth() {
        return !(this.selectedYear === this.currentYear && this.selectedMonth === this.currentMonth);
    }

    canGoToNextMonth() {
        const maxYear = this.currentYear + 1;
        const maxMonth = this.currentMonth;
        return !(this.selectedYear > maxYear || (this.selectedYear === maxYear && this.selectedMonth === maxMonth));
    }
}

// Klasa do zarządzania UI kalendarza
class CalendarUI {
    constructor(state) {
        this.state = state;
        this.elements = this.initializeElements();
        this.bindEvents();
    }

    initializeElements() {
        const elements = {};
        Object.entries(CALENDAR_CONFIG.selectors).forEach(([key, selector]) => {
            elements[key] = document.querySelector(selector);
        });
        return elements;
    }


    bindEvents() {
        this.elements.prevButton?.addEventListener('click', () => this.handlePrevMonth());
        this.elements.nextButton?.addEventListener('click', () => this.handleNextMonth());
        this.bindHoursEvents();
    }

    bindHoursEvents() {
        const hours = document.querySelectorAll(CALENDAR_CONFIG.selectors.hours);
        hours.forEach(hour => {
            hour.addEventListener('click', () => this.handleHourSelection(hour));
        });
    }

    updateMonthLabel() {
        if (this.elements.monthLabel) {
            this.elements.monthLabel.textContent = `${CALENDAR_CONFIG.monthNames[this.state.selectedMonth]} ${this.state.selectedYear}`;
        }
    }

    renderDays() {
        if (!this.elements.days) return;

        const daysInMonth = new Date(this.state.selectedYear, this.state.selectedMonth + 1, 0).getDate();
        const firstDayOfMonth = new Date(this.state.selectedYear, this.state.selectedMonth, 1).getDay();
        const adjustedFirstDay = firstDayOfMonth === 0 ? 6 : firstDayOfMonth - 1;

        this.elements.days.innerHTML = this.generateDaysHTML(daysInMonth, adjustedFirstDay);
        this.bindDaysEvents();

        //this.selectNextAvailableDay();
    }


    bindDaysEvents() {
        const days = this.elements.days.querySelectorAll('li:not(.empty):not(.taken)');
        days.forEach(day => {
            day.addEventListener('click', () => this.handleDaySelection(day));
        });
    }


    async handleDaySelection(dayElement) {
        const activeDay = document.querySelector(".days .active");
        if (activeDay) {
            activeDay.classList.remove("active");
            this.resetSelectedHour();
        }
        dayElement.classList.add("active");
        document.querySelector(".hours")?.classList.remove("disabled");

        //const firstAvailableHour = document.querySelector(".hours li:not(.taken)");
        const selectedDay = dayElement.textContent;
        const hairdresserId = this.elements.hairdresserId?.value;

        if (hairdresserId) {
            await this.fetchAvailableHours(selectedDay, "10:00", hairdresserId);

            const firstAvailableHour = document.querySelector(".hours li:not(.taken)");
            if (firstAvailableHour) {
                firstAvailableHour.classList.add("active");
                // pobieranie usług dla pierwszzej dostępnej usługi
                await this.fetchAvailableHours(selectedDay, firstAvailableHour.textContent, hairdresserId);
                await this.updateAppointmentDate();
            }
        }
    }

    generateDaysHTML(daysInMonth, adjustedFirstDay) {
        let html = '';
        // Dodanie pustych miejsc na początku miesiąca
        for (let i = 0; i < adjustedFirstDay; i++) {
            html += '<li class="empty"></li>';
        }

        // Generowanie dni miesiąca
        for (let day = 1; day <= daysInMonth; day++) {
            const dayDate = new Date(this.state.selectedYear, this.state.selectedMonth, day);
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            const isWeekend = this.isDayWeekend(adjustedFirstDay, day);
            const isPastDay = dayDate < today;
            const classes = this.getDayClasses(isWeekend, isPastDay);

            html += `<li class="${classes}">${day}</li>`;
        }

        return html;
    }

    isDayWeekend(adjustedFirstDay, day) {
        const dayOfWeek = (adjustedFirstDay + day - 1) % 7;
        return dayOfWeek === 5 || dayOfWeek === 6;
    }

    getDayClasses(isWeekend, isPastDay) {
        const classes = [];
        if (isWeekend || isPastDay) classes.push('taken');
        return classes.join(' ');
    }


    resetAppointmentDate() {
        if (this.elements.appointmentDate) {
            this.elements.appointmentDate.value = '';
        }
    }

    resetSelectedHour() {
        const activeHour = document.querySelector(".hours .active");
        if (activeHour) {
            activeHour.classList.remove("active");
        }
        document.querySelector(".hours")?.classList.add("disabled");
    }

    resetSelectedDay() {
        // Usuń zaznaczenie dnia
        const activeDay = document.querySelector(".days .active");
        if (activeDay) {
            activeDay.classList.remove("active");
        }
    }

    // Metody obsługi zdarzeń
    handlePrevMonth() {
        if (!this.state.canGoToPreviousMonth()) return;

        if (this.state.selectedMonth === 0) {
            this.state.selectedMonth = 11;
            this.state.selectedYear--;
        } else {
            this.state.selectedMonth--;
        }
        this.updateServiceOptions();
        this.resetAppointmentDate();
        this.resetSelectedHour();
        this.resetSelectedDay();
        this.updateMonthLabel();
        this.renderDays();
    }

    handleNextMonth() {
        if (!this.state.canGoToNextMonth()) return;

        if (this.state.selectedMonth === 11) {
            this.state.selectedMonth = 0;
            this.state.selectedYear++;
        } else {
            this.state.selectedMonth++;
        }
        this.updateServiceOptions();
        this.resetAppointmentDate();
        this.resetSelectedHour();
        this.resetSelectedDay();
        this.updateMonthLabel();
        this.renderDays();
    }

    async handleHourSelection(hourElement) {
        if (hourElement.classList.contains('taken')) return;

        const activeHour = document.querySelector(".hours .active");
        if (activeHour) activeHour.classList.remove("active");
        hourElement.classList.add("active");

        await this.updateAppointmentDate();
        const selectedHour = hourElement.textContent;
        const selectedDay = document.querySelector(".days .active")?.textContent;
        const hairdresserId = this.elements.hairdresserId?.value;

        if (selectedDay && hairdresserId) {
            await this.fetchAvailableHours(selectedDay, selectedHour, hairdresserId);
        }
    }

    // API i aktualizacja UI
    async fetchAvailableHours(day, hour, hairdresserId) {
        try {
            const response = await fetch(
                `/Appointments/GetReservedHours?hairdresserId=${hairdresserId}&year=${this.state.selectedYear}&month=${this.state.selectedMonth + 1}&day=${day}&hour=${hour}`
            );
            const data = await response.json();

            if (Array.isArray(data.blockedHours)) {
                this.updateServiceOptions(data.availableServices);
                this.updateCalendarWithReservedHours(data.blockedHours);
            }
        } catch (error) {
            console.error("Error fetching hours:", error);
        }
    }

    updateServiceOptions(availableServices = []) {
        if (!this.elements.serviceSelect) return;

        this.elements.serviceSelect.innerHTML = `
            <option value="">Wybierz usługę</option>
            ${availableServices.map(service => `
                <option value="${service.id}">
                    ${service.name} | Czas trwania: ${service.duration / 60} godz. | Cena: ${service.price}
                </option>
            `).join('')}
        `;

        this.bindServiceSelectEvent();
    }

    bindServiceSelectEvent() {
        const hiddenServiceInput = document.querySelector("#SelectedServiceId");
        if (!hiddenServiceInput) return;

        this.elements.serviceSelect.addEventListener('change', function () {
            hiddenServiceInput.value = this.value;
        });
    }

    updateCalendarWithReservedHours(reservedHours) {
        const hours = document.querySelectorAll(".hours li");
        hours.forEach(hour => hour.classList.remove("taken"));

        reservedHours.forEach(reserved => {
            const reservedDate = new Date(reserved);
            if (reservedDate.getMonth() === this.state.selectedMonth &&
                reservedDate.getFullYear() === this.state.selectedYear) {
                const hourElement = [...hours].find(hour =>
                    parseInt(hour.textContent) === reservedDate.getHours()
                );
                if (hourElement) {
                    hourElement.classList.add("taken");
                }
            }
        });
    }

    async updateAppointmentDate() {
        const selectedHour = document.querySelector(".hours .active")?.textContent;
        const selectedDay = document.querySelector(".days .active")?.textContent;

        if (selectedDay && selectedHour && this.elements.appointmentDate) {
            const dateString = `${this.state.selectedYear}-${String(this.state.selectedMonth + 1).padStart(2, '0')}-${String(selectedDay).padStart(2, '0')}T${String(selectedHour).padStart(2, '0')}:00`;
            this.elements.appointmentDate.value = dateString;
        }
    }
}

// Inicjalizacja
document.addEventListener("DOMContentLoaded", () => {
    const calendarState = new CalendarState();
    const calendar = new CalendarUI(calendarState);
    calendar.updateMonthLabel();
    calendar.renderDays();
});

// Funkcja do wyboru fryzjera
async function selectHairdresser(hairdresserId, element) {
    const hairdresserInput = document.querySelector("input[name='HairdresserId']");
    const selectedHairdresserInput = document.getElementById('SelectedHairdresserId');

    if (hairdresserInput && selectedHairdresserInput) {
        hairdresserInput.value = hairdresserId;
        selectedHairdresserInput.value = hairdresserId;
    }

    // Aktualizacja UI
    document.querySelectorAll('.stylist img').forEach(img => img.classList.remove('selected'));
    element.querySelector('img')?.classList.add('selected');

    document.querySelector(".days")?.classList.remove("disabled");

    const activeHour = document.querySelector(".hours .active");
    if (activeHour) {
        activeHour.classList.remove("active");
    }
    

    const selectedDay = document.querySelector(".days .active")?.textContent;
    if (selectedDay) {
        const calendar = new CalendarUI(new CalendarState());
        await calendar.fetchAvailableHours(selectedDay, "10:00", hairdresserId);

        //const firstAvailableHour = document.querySelector(".hours li:not(.taken)");
        //if (firstAvailableHour) {
        //    firstAvailableHour.classList.add("active");
        //    await calendar.updateAppointmentDate();
        //}
    }
}