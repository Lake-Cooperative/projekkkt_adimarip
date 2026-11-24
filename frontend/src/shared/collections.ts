/** Добавляет значение в список или убирает его, если оно уже есть (для чекбоксов). */
export const toggle = <T>(list: T[], value: T): T[] =>
  list.includes(value) ? list.filter(x => x !== value) : [...list, value];
