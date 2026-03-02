from functools import wraps
from flask_jwt_extended import get_jwt, verify_jwt_in_request
from flask import jsonify

# CAPA 1: La configuración (La lista de invitados)
def requiere_rol(niveles_permitidos): 
    # Aquí 'niveles_permitidos' es la lista [1, 4] que tú elijas.

    # CAPA 2: El receptor (El que agarra tu función)
    def decorator(fn): 
        # 'fn' es tu función (ej: borrar_juego). 
        # Este paso es solo para que el decorador sepa qué función está protegiendo.

        # CAPA 3: El Guardia de Seguridad (El "wrapper")
        @wraps(fn) # <--- LA ETIQUETA: Le dice a Flask: "Oye, esto sigue siendo borrar_juego"
        def wrapper(*args, **kwargs):
            verify_jwt_in_request() #verifica que el token JWT esté presente y sea válido. Si no, lanza un error automáticamente.
            claims = get_jwt()
            mi_rol = claims.get('nivel_rol', 0)

            if mi_rol in niveles_permitidos:
                return fn(*args, **kwargs) # "Todo OK, entra a la función original"
            else:
                return jsonify({"msg": "No pasas"}), 403 # "¡A la calle!"

        return wrapper
    return decorator